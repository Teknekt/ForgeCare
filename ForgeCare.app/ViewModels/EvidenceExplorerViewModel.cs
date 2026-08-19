using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.ViewModels;

public sealed class EvidenceExplorerViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlyList<EvidenceExplorerItem> EmptyItems =
        Array.Empty<EvidenceExplorerItem>();

    private static readonly IReadOnlyList<EvidenceExplorerFacet> EmptyFacets =
        Array.Empty<EvidenceExplorerFacet>();

    private readonly IEvidenceRepository _repository;
    private readonly Action<Exception, string> _logFailure;
    private IReadOnlyList<EvidenceExplorerItem> _allItems = EmptyItems;
    private IReadOnlyList<EvidenceExplorerItem> _visibleItems = EmptyItems;
    private IReadOnlyList<EvidenceExplorerFacet> _categoryFacets = EmptyFacets;
    private IReadOnlyList<EvidenceExplorerFacet> _sourceFacets = EmptyFacets;
    private EvidenceExplorerLoadState _loadState = EvidenceExplorerLoadState.NotLoaded;
    private EvidenceCategory? _selectedCategory;
    private EvidenceSource? _selectedSource;
    private EvidenceExplorerFacet? _selectedCategoryFacet;
    private EvidenceExplorerFacet? _selectedSourceFacet;
    private string _searchQuery = string.Empty;
    private EvidenceExplorerItem? _selectedItem;
    private string? _currentSessionId;
    private string? _errorMessage;
    private int? _unsupportedSchemaVersion;
    private int? _supportedSchemaVersion;

    public EvidenceExplorerViewModel(
        IEvidenceRepository repository,
        Action<Exception, string>? logFailure = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logFailure = logFailure ?? CrashLogService.Record;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? CurrentSessionId
    {
        get => _currentSessionId;
        private set => SetField(ref _currentSessionId, value);
    }

    public EvidenceExplorerLoadState LoadState
    {
        get => _loadState;
        private set
        {
            if (SetField(ref _loadState, value))
            {
                OnPropertyChanged(nameof(IsFilteredEmpty));
                OnPropertyChanged(nameof(HasEvidence));
            }
        }
    }

    public IReadOnlyList<EvidenceExplorerItem> AllItems
    {
        get => _allItems;
        private set
        {
            if (SetField(ref _allItems, value))
                OnPropertyChanged(nameof(HasEvidence));
        }
    }

    public IReadOnlyList<EvidenceExplorerItem> VisibleItems
    {
        get => _visibleItems;
        private set
        {
            if (SetField(ref _visibleItems, value))
                OnPropertyChanged(nameof(IsFilteredEmpty));
        }
    }

    public IReadOnlyList<EvidenceExplorerFacet> CategoryFacets
    {
        get => _categoryFacets;
        private set
        {
            if (ReferenceEquals(_categoryFacets, value))
                return;

            _categoryFacets = value;
            EvidenceExplorerFacet? facet = value
                .FirstOrDefault(item => item.Category == SelectedCategory);
            bool selectionChanged = !ReferenceEquals(_selectedCategoryFacet, facet);
            _selectedCategoryFacet = facet;

            OnPropertyChanged();
            if (selectionChanged)
                OnPropertyChanged(nameof(SelectedCategoryFacet));
        }
    }

    public IReadOnlyList<EvidenceExplorerFacet> SourceFacets
    {
        get => _sourceFacets;
        private set
        {
            if (ReferenceEquals(_sourceFacets, value))
                return;

            _sourceFacets = value;
            EvidenceExplorerFacet? facet = value
                .FirstOrDefault(item => item.Source == SelectedSource);
            bool selectionChanged = !ReferenceEquals(_selectedSourceFacet, facet);
            _selectedSourceFacet = facet;

            OnPropertyChanged();
            if (selectionChanged)
                OnPropertyChanged(nameof(SelectedSourceFacet));
        }
    }

    public EvidenceCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetField(ref _selectedCategory, value))
            {
                SynchronizeSelectedCategoryFacet();
                ApplyFilters();
            }
        }
    }

    public EvidenceSource? SelectedSource
    {
        get => _selectedSource;
        set
        {
            if (SetField(ref _selectedSource, value))
            {
                SynchronizeSelectedSourceFacet();
                ApplyFilters();
            }
        }
    }

    public EvidenceExplorerFacet? SelectedCategoryFacet
    {
        get => _selectedCategoryFacet;
        set
        {
            EvidenceExplorerFacet? canonicalFacet = value == null
                ? null
                : CategoryFacets.FirstOrDefault(facet => facet.Category == value.Category);

            if (SetField(ref _selectedCategoryFacet, canonicalFacet))
                SelectedCategory = canonicalFacet?.Category;
        }
    }

    public EvidenceExplorerFacet? SelectedSourceFacet
    {
        get => _selectedSourceFacet;
        set
        {
            EvidenceExplorerFacet? canonicalFacet = value == null
                ? null
                : SourceFacets.FirstOrDefault(facet => facet.Source == value.Source);

            if (SetField(ref _selectedSourceFacet, canonicalFacet))
                SelectedSource = canonicalFacet?.Source;
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            string normalized = value ?? string.Empty;
            if (SetField(ref _searchQuery, normalized))
                ApplyFilters();
        }
    }

    public EvidenceExplorerItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (value != null)
            {
                EvidenceExplorerItem? canonicalItem =
                    VisibleItems.FirstOrDefault(item => item.Id == value.Id);

                if (canonicalItem == null)
                    return;

                value = canonicalItem;
            }

            if (SetField(ref _selectedItem, value))
                OnPropertyChanged(nameof(SelectedId));
        }
    }

    public Guid? SelectedId => SelectedItem?.Id;

    public bool HasEvidence =>
        LoadState == EvidenceExplorerLoadState.Ready && AllItems.Count > 0;

    public bool IsFilteredEmpty =>
        HasEvidence && VisibleItems.Count == 0;

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    public int? UnsupportedSchemaVersion
    {
        get => _unsupportedSchemaVersion;
        private set => SetField(ref _unsupportedSchemaVersion, value);
    }

    public int? SupportedSchemaVersion
    {
        get => _supportedSchemaVersion;
        private set => SetField(ref _supportedSchemaVersion, value);
    }

    public Task LoadSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default) =>
        LoadCoreAsync(sessionId, cancellationToken);

    public Task RefreshAsync(
        string sessionId,
        CancellationToken cancellationToken = default) =>
        LoadCoreAsync(sessionId, cancellationToken);

    public void ClearSearch() => SearchQuery = string.Empty;

    public void ClearFilters()
    {
        bool changed = _selectedCategory != null || _selectedSource != null || _searchQuery.Length > 0;
        _selectedCategory = null;
        _selectedSource = null;
        _searchQuery = string.Empty;

        SynchronizeSelectedCategoryFacet();
        SynchronizeSelectedSourceFacet();

        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(SelectedSource));
        OnPropertyChanged(nameof(SearchQuery));

        if (changed)
            ApplyFilters();
    }

    private async Task LoadCoreAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        bool sessionChanged = !string.Equals(
            CurrentSessionId,
            sessionId,
            StringComparison.Ordinal);

        Guid? selectionToPreserve = sessionChanged ? null : SelectedId;

        if (sessionChanged)
        {
            ResetForNewSession(sessionId);
        }

        ErrorMessage = null;
        UnsupportedSchemaVersion = null;
        SupportedSchemaVersion = null;
        LoadState = EvidenceExplorerLoadState.Loading;

        try
        {
            IReadOnlyList<EvidenceRecord> records = await _repository.GetBySessionAsync(
                sessionId,
                cancellationToken);

            ValidateLoadedRecords(records, sessionId);

            EvidenceExplorerItem[] items = records
                .Select(record => new EvidenceExplorerItem(record))
                .OrderByDescending(item => item.TimestampUtc)
                .ThenBy(item => item.Id)
                .ToArray();

            AllItems = new ReadOnlyCollection<EvidenceExplorerItem>(items);
            CategoryFacets = BuildCategoryFacets(items);
            SourceFacets = BuildSourceFacets(items);
            LoadState = items.Length == 0
                ? EvidenceExplorerLoadState.Empty
                : EvidenceExplorerLoadState.Ready;

            ApplyFilters(selectionToPreserve);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (MalformedEvidenceDocumentException ex)
        {
            ApplyLoadFailure(EvidenceExplorerLoadState.MalformedDocument, ex);
        }
        catch (UnsupportedEvidenceSchemaException ex)
        {
            UnsupportedSchemaVersion = ex.ActualVersion;
            SupportedSchemaVersion = ex.SupportedVersion;
            ApplyLoadFailure(EvidenceExplorerLoadState.UnsupportedSchema, ex);
        }
        catch (Exception ex)
        {
            ApplyLoadFailure(EvidenceExplorerLoadState.LoadError, ex);
        }
    }

    private void ResetForNewSession(string sessionId)
    {
        CurrentSessionId = sessionId;
        _selectedCategory = null;
        _selectedSource = null;
        _searchQuery = string.Empty;
        _selectedCategoryFacet = null;
        _selectedSourceFacet = null;
        SelectedItem = null;
        ClearCollections();

        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(SelectedSource));
        OnPropertyChanged(nameof(SelectedCategoryFacet));
        OnPropertyChanged(nameof(SelectedSourceFacet));
        OnPropertyChanged(nameof(SearchQuery));
    }

    private void ApplyLoadFailure(EvidenceExplorerLoadState state, Exception exception)
    {
        ClearCollections();
        SelectedItem = null;
        ErrorMessage = exception.Message;
        LoadState = state;
        _logFailure(exception, "Evidence Explorer load failure");
    }

    private void ClearCollections()
    {
        AllItems = EmptyItems;
        VisibleItems = EmptyItems;
        CategoryFacets = EmptyFacets;
        SourceFacets = EmptyFacets;
    }

    private void ApplyFilters(Guid? preferredSelection = null)
    {
        Guid? selection = preferredSelection ?? SelectedId;
        string query = SearchQuery.Trim();

        EvidenceExplorerItem[] visible = AllItems
            .Where(item => SelectedCategory == null || item.Category == SelectedCategory)
            .Where(item => SelectedSource == null || item.Source == SelectedSource)
            .Where(item => query.Length == 0 || item.Matches(query))
            .OrderByDescending(item => item.TimestampUtc)
            .ThenBy(item => item.Id)
            .ToArray();

        VisibleItems = new ReadOnlyCollection<EvidenceExplorerItem>(visible);
        SelectedItem = selection == null
            ? visible.FirstOrDefault()
            : visible.FirstOrDefault(item => item.Id == selection.Value) ?? visible.FirstOrDefault();
    }

    private static void ValidateLoadedRecords(
        IReadOnlyList<EvidenceRecord> records,
        string requestedSessionId)
    {
        ArgumentNullException.ThrowIfNull(records);

        foreach (EvidenceRecord? record in records)
        {
            if (record == null)
                throw new InvalidDataException("Evidence repository returned a null record.");

            IReadOnlyList<string> errors = record.Validate();
            if (errors.Count > 0)
            {
                throw new InvalidDataException(
                    $"Evidence record {record.Id} is invalid: {string.Join(" ", errors)}");
            }

            if (!string.Equals(record.SessionId, requestedSessionId, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Evidence record {record.Id} belongs to session '{record.SessionId}', not '{requestedSessionId}'.");
            }
        }
    }

    private static IReadOnlyList<EvidenceExplorerFacet> BuildCategoryFacets(
        IReadOnlyCollection<EvidenceExplorerItem> items)
    {
        var facets = new List<EvidenceExplorerFacet>
        {
            new() { DisplayLabel = "ALL", Count = items.Count }
        };

        facets.AddRange(items
            .GroupBy(item => item.Category)
            .Select(group => new EvidenceExplorerFacet
            {
                DisplayLabel = EvidenceDisplayFormatter.FormatCategory(group.Key).ToUpperInvariant(),
                Count = group.Count(),
                Category = group.Key
            })
            .OrderBy(facet => facet.DisplayLabel, StringComparer.OrdinalIgnoreCase));

        return new ReadOnlyCollection<EvidenceExplorerFacet>(facets);
    }

    private static IReadOnlyList<EvidenceExplorerFacet> BuildSourceFacets(
        IReadOnlyCollection<EvidenceExplorerItem> items)
    {
        var facets = new List<EvidenceExplorerFacet>
        {
            new() { DisplayLabel = "ALL SOURCES", Count = items.Count }
        };

        facets.AddRange(items
            .GroupBy(item => item.Source)
            .Select(group => new EvidenceExplorerFacet
            {
                DisplayLabel = EvidenceDisplayFormatter.FormatSource(group.Key),
                Count = group.Count(),
                Source = group.Key
            })
            .OrderBy(facet => facet.DisplayLabel, StringComparer.OrdinalIgnoreCase));

        return new ReadOnlyCollection<EvidenceExplorerFacet>(facets);
    }

    private void SynchronizeSelectedCategoryFacet()
    {
        EvidenceExplorerFacet? facet = CategoryFacets
            .FirstOrDefault(value => value.Category == SelectedCategory);

        SetField(
            ref _selectedCategoryFacet,
            facet,
            nameof(SelectedCategoryFacet));
    }

    private void SynchronizeSelectedSourceFacet()
    {
        EvidenceExplorerFacet? facet = SourceFacets
            .FirstOrDefault(value => value.Source == SelectedSource);

        SetField(
            ref _selectedSourceFacet,
            facet,
            nameof(SelectedSourceFacet));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
