namespace ForgeCare.App.Models;

public enum EvidenceExplorerLoadState
{
    NotLoaded,
    Loading,
    Ready,
    Empty,
    MalformedDocument,
    UnsupportedSchema,
    LoadError
}
