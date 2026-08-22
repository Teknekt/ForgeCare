using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class WinVerifyTrustStartupSignatureInspector : IStartupSignatureInspector
{
    private const uint WtdUiNone = 2;
    private const uint WtdRevokeNone = 0;
    private const uint WtdChoiceFile = 1;
    private const uint WtdRevocationCheckNone = 0x10;
    private const uint WtdCacheOnlyUrlRetrieval = 0x1000;

    private const int TrustENoSignature = unchecked((int)0x800B0100);
    private const int TrustEBadDigest = unchecked((int)0x80096010);

    private static readonly HashSet<int> UntrustedCodes =
    [
        unchecked((int)0x800B0004), // TRUST_E_SUBJECT_NOT_TRUSTED
        unchecked((int)0x800B0109), // CERT_E_UNTRUSTEDROOT
        unchecked((int)0x800B0111), // TRUST_E_EXPLICIT_DISTRUST
        unchecked((int)0x800B0112)  // CERT_E_UNTRUSTEDCA
    ];

    private static readonly HashSet<int> InvalidCodes =
    [
        unchecked((int)0x80096002), // TRUST_E_NO_SIGNER_CERT
        unchecked((int)0x80096003), // TRUST_E_COUNTER_SIGNER
        unchecked((int)0x80096004), // TRUST_E_CERT_SIGNATURE
        unchecked((int)0x80096005), // TRUST_E_TIME_STAMP
        unchecked((int)0x800B0101), // CERT_E_EXPIRED
        unchecked((int)0x800B0108), // CERT_E_MALFORMED
        unchecked((int)0x800B0110)  // CERT_E_WRONG_USAGE
    ];

    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".com", ".dll", ".sys"
        };

    private static readonly Guid GenericVerifyV2 =
        new("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

    public Task<StartupSignatureInfo> InspectAsync(
        string resolvedPath,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => Inspect(resolvedPath, cancellationToken), cancellationToken);

    private static StartupSignatureInfo Inspect(
        string resolvedPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(resolvedPath))
        {
            return new StartupSignatureInfo
            {
                Status = StartupSignatureStatus.FileMissing
            };
        }

        if (!SupportedExtensions.Contains(Path.GetExtension(resolvedPath)))
        {
            return new StartupSignatureInfo
            {
                Status = StartupSignatureStatus.Unsupported
            };
        }

        try
        {
            int nativeStatus = VerifyTrust(resolvedPath);
            StartupSignatureStatus status = MapNativeStatus(nativeStatus);
            SignerMetadata signer = status is StartupSignatureStatus.Valid or
                StartupSignatureStatus.HashMismatch or
                StartupSignatureStatus.Untrusted or
                StartupSignatureStatus.Invalid
                ? TryReadSigner(resolvedPath)
                : default;

            return new StartupSignatureInfo
            {
                Status = status,
                NativeStatusCode = nativeStatus,
                SignerName = signer.Subject,
                Issuer = signer.Issuer,
                CertificateNotBefore = signer.NotBefore,
                CertificateNotAfter = signer.NotAfter,
                Warning = signer.Warning
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new StartupSignatureInfo
            {
                Status = StartupSignatureStatus.InspectionFailure,
                Warning = $"Authenticode inspection failed ({ex.GetType().Name})."
            };
        }
    }

    private static int VerifyTrust(string path)
    {
        var fileInfo = new WinTrustFileInfo(path);
        IntPtr fileInfoPointer = IntPtr.Zero;

        try
        {
            fileInfoPointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>());
            Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);

            var trustData = new WinTrustData
            {
                Size = (uint)Marshal.SizeOf<WinTrustData>(),
                UiChoice = WtdUiNone,
                RevocationChecks = WtdRevokeNone,
                UnionChoice = WtdChoiceFile,
                FileInfoPointer = fileInfoPointer,
                ProviderFlags = WtdCacheOnlyUrlRetrieval | WtdRevocationCheckNone
            };

            return WinVerifyTrust(
                new IntPtr(-1),
                GenericVerifyV2,
                ref trustData);
        }
        finally
        {
            if (fileInfoPointer != IntPtr.Zero)
                Marshal.FreeHGlobal(fileInfoPointer);

            fileInfo.Dispose();
        }
    }

    private static StartupSignatureStatus MapNativeStatus(int nativeStatus)
    {
        if (nativeStatus == 0)
            return StartupSignatureStatus.Valid;
        if (nativeStatus == TrustENoSignature)
            return StartupSignatureStatus.NotSigned;
        if (nativeStatus == TrustEBadDigest)
            return StartupSignatureStatus.HashMismatch;
        if (UntrustedCodes.Contains(nativeStatus))
            return StartupSignatureStatus.Untrusted;
        if (InvalidCodes.Contains(nativeStatus))
            return StartupSignatureStatus.Invalid;
        return StartupSignatureStatus.InspectionFailure;
    }

    private static SignerMetadata TryReadSigner(string path)
    {
        try
        {
#pragma warning disable SYSLIB0057 // There is no non-obsolete API that extracts an embedded Authenticode signer certificate.
            using X509Certificate certificate = X509Certificate.CreateFromSignedFile(path);
#pragma warning restore SYSLIB0057
            using var certificate2 = new X509Certificate2(certificate);
            return new SignerMetadata(
                certificate2.GetNameInfo(X509NameType.SimpleName, false),
                certificate2.Issuer,
                certificate2.NotBefore,
                certificate2.NotAfter,
                null);
        }
        catch (Exception ex)
        {
            return new SignerMetadata(
                null,
                null,
                null,
                null,
                $"Signer metadata was unavailable ({ex.GetType().Name}); the Authenticode status is unchanged.");
        }
    }

    [DllImport("wintrust.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int WinVerifyTrust(
        IntPtr windowHandle,
        [MarshalAs(UnmanagedType.LPStruct)] Guid actionId,
        ref WinTrustData trustData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class WinTrustFileInfo : IDisposable
    {
        public uint Size = (uint)Marshal.SizeOf<WinTrustFileInfo>();
        public IntPtr FilePathPointer;
        public IntPtr FileHandle;
        public IntPtr KnownSubject;

        public WinTrustFileInfo(string path)
        {
            FilePathPointer = Marshal.StringToCoTaskMemUni(path);
        }

        public void Dispose()
        {
            if (FilePathPointer == IntPtr.Zero)
                return;

            Marshal.FreeCoTaskMem(FilePathPointer);
            FilePathPointer = IntPtr.Zero;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustData
    {
        public uint Size;
        public IntPtr PolicyCallbackData;
        public IntPtr SipClientData;
        public uint UiChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public IntPtr FileInfoPointer;
        public uint StateAction;
        public IntPtr StateData;
        public IntPtr UrlReference;
        public uint ProviderFlags;
        public uint UiContext;
        public IntPtr SignatureSettings;
    }

    private readonly record struct SignerMetadata(
        string? Subject,
        string? Issuer,
        DateTime? NotBefore,
        DateTime? NotAfter,
        string? Warning);
}
