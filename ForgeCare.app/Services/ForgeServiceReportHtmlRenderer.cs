using System.Globalization;
using System.Net;
using System.Text;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public sealed class ForgeServiceReportHtmlRenderer
{
    public string Render(ForgeServiceReportModel report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var html = new StringBuilder(32_768);
        AppendDocumentStart(html);
        AppendHeader(html, report.SessionSummary);
        html.AppendLine("<main>");
        AppendSessionSummary(html, report.SessionSummary);
        AppendActivities(html, report.Activities);
        AppendActions(html, report.Actions);
        AppendVerification(html, report.Actions);
        AppendUnresolved(html, report.UnresolvedItems);
        AppendEvidence(html, report.EvidenceReferences);
        AppendCheckpoints(html, report.Checkpoints);
        AppendWarnings(html, report.Warnings);
        html.AppendLine("</main>");
        html.AppendLine("<footer><p>ForgeCare — Technician Edition</p><p>Report generated from the recorded ForgeCare service session.</p></footer>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    private static void AppendDocumentStart(StringBuilder html)
    {
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("<title>ForgeCare Service Report</title>");
        html.AppendLine("<style>");
        html.AppendLine("""
            :root{color-scheme:dark;--bg:#101214;--surface:#181b1f;--surface2:#20242a;--line:#343a42;--text:#eef0f2;--muted:#a9b0b8;--gold:#d7ad55;--good:#79c895;--warn:#dfb660;--bad:#df7b76;--info:#84a9d8}
            *{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:15px/1.55 "Segoe UI",Arial,sans-serif}header,main,footer{max-width:1180px;margin:0 auto;padding:28px 36px}header{padding-top:42px;border-bottom:1px solid var(--line)}.eyebrow,.label{color:var(--gold);font-size:.76rem;font-weight:700;letter-spacing:.14em;text-transform:uppercase}h1{margin:.2rem 0;font-size:2rem;letter-spacing:.04em}h2{margin:0 0 18px;font-size:1.2rem;letter-spacing:.07em;text-transform:uppercase}h3{margin:0 0 10px;font-size:1rem}p{margin:.35rem 0}.muted{color:var(--muted)}section{margin:0 0 22px;padding:22px;border:1px solid var(--line);border-radius:14px;background:var(--surface);break-inside:avoid}.summary-grid,.checkpoint-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:12px}.metric,.checkpoint,.action-card{padding:14px;border:1px solid var(--line);border-radius:10px;background:var(--surface2);break-inside:avoid}.metric strong{display:block;margin-top:4px;font-size:1.05rem}.activity-list,.verification-list,.unresolved-list{margin:0;padding:0;list-style:none}.activity-list li,.verification-list li,.unresolved-list li{padding:11px 0;border-top:1px solid var(--line)}.activity-list li:first-child,.verification-list li:first-child,.unresolved-list li:first-child{border-top:0}.actions{display:grid;gap:12px}.action-header{display:flex;align-items:flex-start;justify-content:space-between;gap:16px}.state{display:inline-block;padding:3px 8px;border:1px solid currentColor;border-radius:999px;font-size:.73rem;font-weight:700;letter-spacing:.05em;text-transform:uppercase}.state-good{color:var(--good)}.state-warn{color:var(--warn)}.state-bad{color:var(--bad)}.state-info{color:var(--info)}.state-muted{color:var(--muted)}.detail-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:10px;margin-top:12px}.detail-grid div{min-width:0}.technical{font-family:Consolas,"Courier New",monospace;font-size:.82rem;color:var(--muted);overflow-wrap:anywhere}table{width:100%;border-collapse:collapse}caption{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}th,td{padding:10px 8px;border-top:1px solid var(--line);text-align:left;vertical-align:top;overflow-wrap:anywhere}thead th{border-top:0;color:var(--gold);font-size:.75rem;letter-spacing:.08em;text-transform:uppercase}.note{padding:12px;border-left:3px solid var(--gold);background:var(--surface2);color:var(--muted)}footer{border-top:1px solid var(--line);color:var(--muted);font-size:.85rem}.empty{color:var(--muted);font-style:italic}@media(max-width:760px){header,main,footer{padding:22px 18px}.action-header{display:block}.action-header .state{margin-top:8px}table{font-size:.86rem}}
            @media print{:root{color-scheme:light;--bg:#fff;--surface:#fff;--surface2:#f5f5f5;--line:#c9c9c9;--text:#151515;--muted:#555;--gold:#725511;--good:#216b39;--warn:#74540b;--bad:#8a2823;--info:#24578d}body{font-size:11pt}header,main,footer{max-width:none;padding:16px 20px}section{box-shadow:none;break-inside:auto}.action-card,.checkpoint,table tr{break-inside:avoid}a{color:inherit;text-decoration:none}}
            """);
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
    }

    private static void AppendHeader(StringBuilder html, ServiceReportSessionSummary summary)
    {
        html.AppendLine("<header>");
        html.AppendLine("<div class=\"eyebrow\">ForgeCare · Technician Edition</div>");
        html.AppendLine("<h1>Professional Service Report</h1>");
        html.Append("<p class=\"muted\">Service Session <span class=\"technical\">")
            .Append(Encode(DisplaySessionId(summary.SessionId)))
            .AppendLine("</span></p>");
        html.AppendLine("</header>");
    }

    private static void AppendSessionSummary(StringBuilder html, ServiceReportSessionSummary summary)
    {
        OpenSection(html, "session-summary", "Service Session");
        html.AppendLine("<div class=\"summary-grid\">");
        AppendMetric(html, "Session reference", DisplaySessionId(summary.SessionId), technical: true);
        AppendMetric(html, "Started", FormatRecordedTime(summary.StartedAt));
        AppendMetric(html, "Last updated", FormatRecordedTime(summary.UpdatedAt));
        AppendMetric(html, "Diagnostic activity", summary.DiagnosticActivityCount.ToString(CultureInfo.InvariantCulture));
        AppendMetric(html, "Technician action targets", summary.StartupActionTargetCount.ToString(CultureInfo.InvariantCulture));
        AppendMetric(html, "Evidence references", summary.EvidenceReferenceCount.ToString(CultureInfo.InvariantCulture));
        AppendMetric(html, "Latest checkpoint", summary.LatestCheckpointAt is DateTime value ? FormatRecordedTime(value) : "Not recorded");
        html.AppendLine("</div></section>");
    }

    private static void AppendActivities(StringBuilder html, IReadOnlyList<ServiceReportActivity> activities)
    {
        OpenSection(html, "diagnostic-activity", "Diagnostic Activity");
        ServiceReportActivity[] diagnostic = activities
            .Where(value => value.Kind == ServiceReportActivityKind.DiagnosticActivity)
            .ToArray();
        if (diagnostic.Length == 0)
            AppendEmpty(html, "No diagnostic activity was recorded.");
        else
            AppendActivityList(html, diagnostic);

        ServiceReportActivity[] other = activities
            .Where(value => value.Kind != ServiceReportActivityKind.DiagnosticActivity)
            .ToArray();
        if (other.Length > 0)
        {
            html.AppendLine("<h3>Other recorded legacy activity</h3>");
            html.AppendLine("<p class=\"muted\">These entries are historical activity records and do not carry typed Verification 2.0 traceability.</p>");
            AppendActivityList(html, other);
        }
        html.AppendLine("</section>");
    }

    private static void AppendActivityList(StringBuilder html, IEnumerable<ServiceReportActivity> activities)
    {
        html.AppendLine("<ul class=\"activity-list\">");
        foreach (ServiceReportActivity activity in activities)
        {
            html.Append("<li><strong>").Append(Encode(activity.Title)).Append("</strong> <span class=\"state state-muted\">")
                .Append(Encode(ActivityKindDisplay(activity.Kind))).Append("</span><br><span class=\"muted\">")
                .Append(Encode(activity.Category)).Append(" · ").Append(Encode(FormatRecordedTime(activity.Timestamp)))
                .AppendLine("</span></li>");
        }
        html.AppendLine("</ul>");
    }

    private static void AppendActions(StringBuilder html, IReadOnlyList<ServiceReportAction> actions)
    {
        OpenSection(html, "technician-actions", "Technician Actions");
        if (actions.Count == 0)
        {
            AppendEmpty(html, "No technician-controlled startup actions were recorded.");
            html.AppendLine("</section>");
            return;
        }

        html.AppendLine("<div class=\"actions\">");
        foreach (ServiceReportAction action in actions)
        {
            html.AppendLine("<article class=\"action-card\">");
            html.Append("<div class=\"action-header\"><div><div class=\"label\">Startup ")
                .Append(Encode(OperationDisplay(action.Operation))).Append("</div><h3>")
                .Append(Encode(action.TargetDisplayName)).Append("</h3></div><span class=\"state ")
                .Append(ExecutionCss(action.ExecutionStatus)).Append("\">")
                .Append(Encode(action.ExecutionDisplay)).AppendLine("</span></div>");
            html.AppendLine("<div class=\"detail-grid\">");
            AppendDetail(html, "Executed", FormatUtc(action.ExecutedAtUtc));
            AppendDetail(html, "Source", action.SourceDisplay);
            AppendDetail(html, "Expected state", action.ExpectedState);
            AppendDetail(html, "Traceability", TraceabilityDisplay(action.TraceabilityStatus));
            AppendDetail(html, "Recovery", action.HasRecoveryReference ? "Recovery reference available" : "No recovery reference recorded");
            AppendDetail(html, "Technical reference", ShortReference(action.ReceiptId), technical: true);
            html.AppendLine("</div>");
            if (action.TraceabilityStatus == ServiceReportTraceabilityStatus.Superseded)
                html.AppendLine("<p class=\"note\">Superseded by a later opposite startup operation.</p>");
            else if (!string.IsNullOrWhiteSpace(action.SupersedesReceiptId))
                html.AppendLine("<p class=\"note\">This operation superseded an earlier opposite startup operation.</p>");
            html.AppendLine("</article>");
        }
        html.AppendLine("</div></section>");
    }

    private static void AppendVerification(StringBuilder html, IReadOnlyList<ServiceReportAction> actions)
    {
        OpenSection(html, "verification", "Verification");
        if (actions.Count == 0)
        {
            AppendEmpty(html, "No verification results were recorded.");
            html.AppendLine("</section>");
            return;
        }

        html.AppendLine("<ul class=\"verification-list\">");
        foreach (ServiceReportAction action in actions)
        {
            html.Append("<li><div class=\"action-header\"><strong>").Append(Encode(action.TargetDisplayName))
                .Append("</strong><span class=\"state ").Append(VerificationCss(action.VerificationStatus, action.TraceabilityStatus))
                .Append("\">").Append(Encode(VerificationDisplay(action.VerificationStatus, action.TraceabilityStatus)))
                .AppendLine("</span></div>");
            if (action.Verifications.Count == 0)
            {
                html.Append("<p class=\"muted\">").Append(Encode(VerificationEmptySummary(action))).AppendLine("</p>");
            }
            else
            {
                foreach (ServiceReportVerification verification in action.Verifications)
                {
                    html.Append("<p><strong>").Append(Encode(verification.StatusDisplay)).Append(":</strong> ")
                        .Append(Encode(verification.Summary)).Append(" <span class=\"muted\">Observed ")
                        .Append(Encode(FormatUtc(verification.ObservationTimestampUtc))).AppendLine("</span></p>");
                }
            }
            html.AppendLine("</li>");
        }
        html.AppendLine("</ul></section>");
    }

    private static void AppendUnresolved(StringBuilder html, IReadOnlyList<ServiceReportUnresolvedItem> unresolved)
    {
        OpenSection(html, "unresolved", "Unresolved Items · Attention Required");
        if (unresolved.Count == 0)
            AppendEmpty(html, "No unresolved verification items were identified.");
        else
        {
            html.AppendLine("<p class=\"muted\">These items require review or a later observation. Their presence does not by itself indicate danger.</p>");
            html.AppendLine("<ul class=\"unresolved-list\">");
            foreach (ServiceReportUnresolvedItem item in unresolved)
            {
                html.Append("<li><strong>").Append(Encode(UnresolvedKindDisplay(item.Kind))).Append(":</strong> ")
                    .Append(Encode(item.Summary)).Append(" <span class=\"muted\">")
                    .Append(Encode(FormatRecordedTime(item.Timestamp))).AppendLine("</span></li>");
            }
            html.AppendLine("</ul>");
        }
        html.AppendLine("</section>");
    }

    private static void AppendEvidence(StringBuilder html, IReadOnlyList<ServiceReportEvidenceReference> evidence)
    {
        OpenSection(html, "evidence-references", "Evidence References");
        html.AppendLine("<p class=\"note\">Evidence references document observations recorded during the service session and are not automatically equivalent to action justification.</p>");
        if (evidence.Count == 0)
            AppendEmpty(html, "No Evidence references were supplied.");
        else
        {
            html.AppendLine("<table><caption>Evidence references recorded during the service session</caption><thead><tr><th scope=\"col\">Reference</th><th scope=\"col\">Observed</th><th scope=\"col\">Source</th><th scope=\"col\">Category</th><th scope=\"col\">Subject</th><th scope=\"col\">Severity</th><th scope=\"col\">Confidence</th></tr></thead><tbody>");
            foreach (ServiceReportEvidenceReference item in evidence)
            {
                html.Append("<tr><td class=\"technical\">").Append(Encode(item.Reference)).Append("</td><td>")
                    .Append(Encode(FormatUtc(item.TimestampUtc))).Append("</td><td>").Append(Encode(item.SourceDisplay))
                    .Append("</td><td>").Append(Encode(item.CategoryDisplay)).Append("</td><td>")
                    .Append(Encode(item.SubjectDisplay)).Append("</td><td>").Append(Encode(item.SeverityDisplay))
                    .Append("</td><td>").Append(Encode(item.ConfidenceDisplay)).AppendLine("</td></tr>");
            }
            html.AppendLine("</tbody></table>");
        }
        html.AppendLine("</section>");
    }

    private static void AppendCheckpoints(StringBuilder html, IReadOnlyList<ServiceReportCheckpoint> checkpoints)
    {
        OpenSection(html, "session-checkpoints", "Session Checkpoints · Final State");
        html.AppendLine("<p class=\"muted\">Checkpoint values are recorded session context. Differences are not attributed to a specific action.</p>");
        if (checkpoints.Count == 0)
        {
            AppendEmpty(html, "No session checkpoints were recorded.");
            html.AppendLine("</section>");
            return;
        }

        html.AppendLine("<div class=\"checkpoint-grid\">");
        AppendCheckpoint(html, "Initial recorded state", checkpoints[0]);
        if (checkpoints.Count > 1)
            AppendCheckpoint(html, "Latest recorded state", checkpoints[^1]);
        html.AppendLine("</div></section>");
    }

    private static void AppendCheckpoint(StringBuilder html, string title, ServiceReportCheckpoint checkpoint)
    {
        html.Append("<article class=\"checkpoint\"><h3>").Append(Encode(title)).AppendLine("</h3><div class=\"detail-grid\">");
        AppendDetail(html, "Recorded", FormatRecordedTime(checkpoint.Timestamp));
        AppendDetail(html, "Health", $"{checkpoint.HealthScore.ToString(CultureInfo.InvariantCulture)}/100 · {checkpoint.HealthRating}");
        AppendDetail(html, "System drive free", $"{checkpoint.SystemDriveFreeGb.ToString("0.#", CultureInfo.InvariantCulture)} GB · {checkpoint.StorageFreePercent.ToString("0.#", CultureInfo.InvariantCulture)}%");
        AppendDetail(html, "Memory available", $"{checkpoint.AvailableMemoryGb.ToString("0.#", CultureInfo.InvariantCulture)} GB · {checkpoint.MemoryAvailablePercent.ToString("0.#", CultureInfo.InvariantCulture)}%");
        AppendDetail(html, "Startup entries", checkpoint.StartupCount.ToString(CultureInfo.InvariantCulture));
        html.AppendLine("</div></article>");
    }

    private static void AppendWarnings(StringBuilder html, IReadOnlyList<string> warnings)
    {
        OpenSection(html, "report-data-notes", "Report Data Notes");
        if (warnings.Count == 0)
            AppendEmpty(html, "No report data notes were recorded.");
        else
        {
            html.AppendLine("<ul class=\"unresolved-list\">");
            foreach (string warning in warnings)
                html.Append("<li>").Append(Encode(warning)).AppendLine("</li>");
            html.AppendLine("</ul>");
        }
        html.AppendLine("</section>");
    }

    private static void OpenSection(StringBuilder html, string id, string heading)
    {
        html.Append("<section id=\"").Append(id).Append("\"><h2>").Append(heading).AppendLine("</h2>");
    }

    private static void AppendMetric(StringBuilder html, string label, string value, bool technical = false)
    {
        html.Append("<div class=\"metric\"><span class=\"label\">").Append(label).Append("</span><strong")
            .Append(technical ? " class=\"technical\"" : string.Empty).Append('>')
            .Append(Encode(value)).AppendLine("</strong></div>");
    }

    private static void AppendDetail(StringBuilder html, string label, string value, bool technical = false)
    {
        html.Append("<div><span class=\"label\">").Append(label).Append("</span><p")
            .Append(technical ? " class=\"technical\"" : string.Empty).Append('>')
            .Append(Encode(value)).AppendLine("</p></div>");
    }

    private static void AppendEmpty(StringBuilder html, string value) =>
        html.Append("<p class=\"empty\">").Append(Encode(value)).AppendLine("</p>");

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    private static string FormatUtc(DateTime value) => value.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
    private static string FormatRecordedTime(DateTime value) => value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    private static string DisplaySessionId(string value) => string.IsNullOrWhiteSpace(value) ? "Unavailable" : value;
    private static string ShortReference(string value) => string.IsNullOrWhiteSpace(value) ? "Unavailable" : value[..Math.Min(10, value.Length)];
    private static string OperationDisplay(StartupVerificationOperation value) => value == StartupVerificationOperation.Disable ? "Disable" : "Restore";

    private static string ActivityKindDisplay(ServiceReportActivityKind value) => value switch
    {
        ServiceReportActivityKind.DiagnosticActivity => "Diagnostic Activity",
        ServiceReportActivityKind.SystemChange => "Legacy System Change",
        ServiceReportActivityKind.Recovery => "Legacy Recovery",
        _ => "Legacy Activity"
    };

    private static string TraceabilityDisplay(ServiceReportTraceabilityStatus value) => value switch
    {
        ServiceReportTraceabilityStatus.ExecutedAndVerified => "Executed and Verified",
        ServiceReportTraceabilityStatus.ExecutedNotVerified => "Executed — Expected State Not Observed",
        ServiceReportTraceabilityStatus.ExecutedInconclusive => "Executed — Verification Inconclusive",
        ServiceReportTraceabilityStatus.PendingVerification => "Executed — Verification Pending",
        ServiceReportTraceabilityStatus.ExecutionFailed => "Execution Failed",
        ServiceReportTraceabilityStatus.NotExecuted => "Not Executed",
        ServiceReportTraceabilityStatus.Superseded => "Superseded",
        _ => "Not Eligible"
    };

    private static string VerificationDisplay(
        ServiceReportVerificationStatus status,
        ServiceReportTraceabilityStatus traceability) => status switch
    {
        ServiceReportVerificationStatus.Verified => "Verified",
        ServiceReportVerificationStatus.NotVerified => "Not Verified",
        ServiceReportVerificationStatus.Inconclusive => "Inconclusive",
        ServiceReportVerificationStatus.NotEligible => "Not Eligible",
        ServiceReportVerificationStatus.Pending => "Pending",
        _ when traceability == ServiceReportTraceabilityStatus.Superseded => "Superseded",
        _ => "No Verification Claim"
    };

    private static string VerificationEmptySummary(ServiceReportAction action) => action.TraceabilityStatus switch
    {
        ServiceReportTraceabilityStatus.PendingVerification => "Awaiting a later valid observation.",
        ServiceReportTraceabilityStatus.Superseded => "A later opposite startup operation superseded this expected state.",
        ServiceReportTraceabilityStatus.NotEligible => "No verification claim applied.",
        ServiceReportTraceabilityStatus.ExecutionFailed => "No verification claim was made because execution failed.",
        ServiceReportTraceabilityStatus.NotExecuted => "No verification claim was made because the target was not changed.",
        _ => "No verification result was recorded."
    };

    private static string UnresolvedKindDisplay(ServiceReportUnresolvedKind value) => value switch
    {
        ServiceReportUnresolvedKind.ExecutionFailed => "Execution failed",
        ServiceReportUnresolvedKind.NotExecuted => "Not executed",
        ServiceReportUnresolvedKind.ExpectedStateNotObserved => "Expected state not observed",
        ServiceReportUnresolvedKind.InconclusiveObservation => "Inconclusive observation",
        ServiceReportUnresolvedKind.PendingVerification => "Pending verification",
        ServiceReportUnresolvedKind.OrphanVerification => "Unlinked verification",
        _ => "Incomplete traceability"
    };

    private static string ExecutionCss(ServiceReportExecutionStatus value) => value switch
    {
        ServiceReportExecutionStatus.Executed => "state-good",
        ServiceReportExecutionStatus.Failed => "state-bad",
        ServiceReportExecutionStatus.Blocked => "state-warn",
        _ => "state-muted"
    };

    private static string VerificationCss(
        ServiceReportVerificationStatus status,
        ServiceReportTraceabilityStatus traceability) => status switch
    {
        ServiceReportVerificationStatus.Verified => "state-good",
        ServiceReportVerificationStatus.NotVerified => "state-bad",
        ServiceReportVerificationStatus.Inconclusive or ServiceReportVerificationStatus.Pending => "state-warn",
        ServiceReportVerificationStatus.NotEligible => "state-info",
        _ when traceability == ServiceReportTraceabilityStatus.Superseded => "state-info",
        _ => "state-muted"
    };
}
