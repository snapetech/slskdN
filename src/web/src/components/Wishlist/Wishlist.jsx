import './Wishlist.css';
import { urlBase } from '../../config';
import {
  buildWishlistRequestReviewPacket,
  buildWishlistRequestSummary,
  formatWishlistRequestReviewPacket,
  getWishlistRequestState,
  getRunnableWishlistRequests,
} from '../../lib/acquisitionRequests';
import * as wishlistAPI from '../../lib/wishlist';
import * as searchesAPI from '../../lib/searches';
import * as optionsAPI from '../../lib/options';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { toast } from 'react-toastify';
import {
  Button,
  Checkbox,
  Confirm,
  Form,
  Header,
  Icon,
  Label,
  Modal,
  Popup,
  Segment,
  Table,
} from 'semantic-ui-react';

const formatDate = (dateString) => {
  if (!dateString) return 'Never';
  const date = new Date(dateString);
  if (Number.isNaN(date.getTime())) return 'Never';
  return date.toLocaleString();
};

const getUnseenCount = (item) => {
  if (!item.lastSearchId || !item.lastSearchedAt) return 0;
  if (!item.lastViewedAt) return item.totalSearchCount || 0;
  const lastViewed = new Date(item.lastViewedAt).getTime();
  const lastSearch = new Date(item.lastSearchedAt).getTime();
  return lastSearch > lastViewed && item.lastMatchCount > 0 ? item.lastMatchCount : 0;
};

const getSearchLink = (item, searchId = item.lastSearchId) => {
  const params = new URLSearchParams();

  if (item.filter) {
    params.set('filter', item.filter);
  }

  const suffix = params.toString() ? `?${params.toString()}` : '';
  return `${urlBase}/searches/${encodeURIComponent(searchId)}${suffix}`;
};

const WishlistItemRow = ({
  item,
  onDelete,
  onEdit,
  onRunSearch,
  onMarkViewed,
  selected,
  onSelect,
}) => {
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [running, setRunning] = useState(false);
  const [showSearches, setShowSearches] = useState(false);
  const [relatedSearches, setRelatedSearches] = useState([]);
  const [loadingSearches, setLoadingSearches] = useState(false);
  const [expandedSearchId, setExpandedSearchId] = useState(null);
  const [expandedSearchResults, setExpandedSearchResults] = useState([]);
  const [loadingResults, setLoadingResults] = useState(false);
  const requestState = getWishlistRequestState(item, []);
  const unseenCount = getUnseenCount(item);

  const handleRunSearch = async () => {
    setRunning(true);
    try {
      const result = await onRunSearch(item.id);
      toast.success(`Search completed with ${result.responseCount} results`);
    } catch (error) {
      toast.error(`Search failed: ${error.message}`);
    } finally {
      setRunning(false);
    }
  };

  const handleToggleSearches = async () => {
    const next = !showSearches;
    setShowSearches(next);
    if (next && relatedSearches.length === 0) {
      setLoadingSearches(true);
      try {
        const searches = await wishlistAPI.getSearches(item.id);
        setRelatedSearches(searches);
        if (unseenCount > 0) {
          await onMarkViewed(item.id);
        }
      } catch (error) {
        toast.error(`Failed to load searches: ${error.message}`);
      } finally {
        setLoadingSearches(false);
      }
    }
  };

  const handleMarkViewedClick = async () => {
    await onMarkViewed(item.id);
    toast.info(`Marked "${item.searchText}" as viewed`);
  };

  const handleToggleResults = async (searchId) => {
    if (expandedSearchId === searchId) {
      setExpandedSearchId(null);
      setExpandedSearchResults([]);
      return;
    }
    setExpandedSearchId(searchId);
    setLoadingResults(true);
    try {
      const results = await searchesAPI.getResponses({ id: searchId });
      setExpandedSearchResults(results);
    } catch (error) {
      toast.error(`Failed to load results: ${error.message}`);
      setExpandedSearchResults([]);
    } finally {
      setLoadingResults(false);
    }
  };

  return (
    <>
      <Table.Row>
        <Table.Cell>
          <Popup
            content="Select for bulk actions"
            position="top center"
            trigger={
              <Checkbox
                checked={selected}
                onChange={(_, { checked }) => onSelect(item.id, checked)}
              />
            }
          />
        </Table.Cell>
        <Table.Cell>
          <Icon
            color={item.enabled ? 'green' : 'grey'}
            name={item.enabled ? 'check circle' : 'circle outline'}
            style={{ marginRight: '0.5em' }}
          />
          <strong>{item.searchText}</strong>
          {item.filter && (
            <div className="wishlist-filter">Filter: {item.filter}</div>
          )}
        </Table.Cell>
        <Table.Cell textAlign="center">
          <Popup
            content="Auto-download best matches"
            trigger={
              <Icon
                color={item.autoDownload ? 'green' : 'grey'}
                name={item.autoDownload ? 'download' : 'download'}
              />
            }
          />
        </Table.Cell>
        <Table.Cell>{formatDate(item.lastSearchedAt)}</Table.Cell>
        <Table.Cell textAlign="center">
          {item.lastMatchCount}
          {unseenCount > 0 && (
            <Popup
              content={`${unseenCount} new result(s) since you last viewed this item`}
              trigger={
                <Label
                  color="red"
                  size="mini"
                  style={{ marginLeft: '0.5em' }}
                >
                  {unseenCount} new
                </Label>
              }
            />
          )}
        </Table.Cell>
        <Table.Cell textAlign="center">{item.totalSearchCount}</Table.Cell>
        <Table.Cell>
          <Popup
            content={requestState.summary}
            position="top center"
            trigger={
              <Label color={requestState.color}>
                {requestState.label}
              </Label>
            }
          />
        </Table.Cell>
        <Table.Cell>
          {item.lastSearchId && (
            <Link
              onClick={() => unseenCount > 0 && onMarkViewed(item.id)}
              to={getSearchLink(item)}
            >
              <Popup
                content={item.filter
                  ? 'View the latest search results with this wishlist filter already applied.'
                  : 'View the latest search results for this wishlist item.'}
                position="top center"
                trigger={
                  <Button
                    compact
                    icon="search"
                    size="tiny"
                    title="View last search results"
                  />
                }
              />
            </Link>
          )}
          {unseenCount > 0 && (
            <Popup
              content="Clear this item's new-results badge without opening its search history."
              position="top center"
              trigger={
                <Button
                  compact
                  icon="check"
                  onClick={handleMarkViewedClick}
                  size="tiny"
                  title="Mark viewed"
                />
              }
            />
          )}
          <Button
            compact
            icon={showSearches ? 'angle up' : 'angle down'}
            loading={loadingSearches}
            onClick={handleToggleSearches}
            size="tiny"
            title={showSearches ? 'Hide search history' : 'Show search history'}
          />
          <Button
            compact
            icon="play"
            loading={running}
            onClick={handleRunSearch}
            primary
            size="tiny"
            title="Run search now"
          />
          <Button
            compact
            icon="edit"
            onClick={() => onEdit(item)}
            size="tiny"
            title="Edit"
          />
          <Button
            color="red"
            compact
            icon="trash"
            onClick={() => setConfirmDelete(true)}
            size="tiny"
            title="Delete"
          />
          <Confirm
            cancelButton="Cancel"
            confirmButton="Delete"
            content={`Delete wishlist item "${item.searchText}"?`}
            header="Confirm Delete"
            onCancel={() => setConfirmDelete(false)}
            onConfirm={() => {
              setConfirmDelete(false);
              onDelete(item.id);
            }}
            open={confirmDelete}
            size="mini"
          />
        </Table.Cell>
      </Table.Row>
      {showSearches && (
        <Table.Row>
          <Table.Cell colSpan="8" style={{ background: 'rgba(0,0,0,0.03)' }}>
            {loadingSearches ? (
              <Icon name="spinner" loading />
            ) : relatedSearches.length === 0 ? (
              <span style={{ color: '#999', fontStyle: 'italic' }}>
                No linked search history for this item yet.
                {item.lastSearchId && (
                  <>
                    {' '}
                    <Link
                      onClick={() => unseenCount > 0 && onMarkViewed(item.id)}
                      to={getSearchLink(item)}
                    >
                      Open latest search result.
                    </Link>
                  </>
                )}
              </span>
            ) : (
              <>
                <Table compact size="small" basic="very">
                  <Table.Header>
                    <Table.Row>
                      <Table.HeaderCell>Search</Table.HeaderCell>
                      <Table.HeaderCell>Source</Table.HeaderCell>
                      <Table.HeaderCell>Responses</Table.HeaderCell>
                      <Table.HeaderCell>Files</Table.HeaderCell>
                      <Table.HeaderCell>Started</Table.HeaderCell>
                      <Table.HeaderCell />
                    </Table.Row>
                  </Table.Header>
                  <Table.Body>
                    {relatedSearches.map((s) => (
                      <Table.Row key={s.id}>
                        <Table.Cell>{s.searchText}</Table.Cell>
                        <Table.Cell>
                          <Label
                            color={s.source === 'wishlist' ? 'blue' : s.source === 'auto-replace' ? 'orange' : 'grey'}
                            size="mini"
                          >
                            {s.source || 'manual'}
                          </Label>
                        </Table.Cell>
                        <Table.Cell>{s.responseCount}</Table.Cell>
                        <Table.Cell>{s.fileCount}</Table.Cell>
                        <Table.Cell>{formatDate(s.startedAt)}</Table.Cell>
                        <Table.Cell>
                          <Link
                            onClick={() => unseenCount > 0 && onMarkViewed(item.id)}
                            to={getSearchLink(item, s.id)}
                          >
                            <Popup
                              content={item.filter
                                ? 'View full search page with this wishlist filter already applied.'
                                : 'View full search page.'}
                              position="top center"
                              trigger={
                                <Button
                                  compact
                                  icon="external"
                                  size="mini"
                                />
                              }
                            />
                          </Link>
                          <Popup
                            content={expandedSearchId === s.id ? 'Hide results' : 'Show results inline'}
                            position="top center"
                            trigger={
                              <Button
                                compact
                                icon={expandedSearchId === s.id ? 'angle up' : 'angle down'}
                                loading={loadingResults && expandedSearchId === s.id}
                                onClick={() => handleToggleResults(s.id)}
                                size="mini"
                              />
                            }
                          />
                        </Table.Cell>
                      </Table.Row>
                    ))}
                  </Table.Body>
                </Table>
                {expandedSearchId && (
                  <div style={{ marginTop: '0.75em', padding: '0.5em', background: 'rgba(255,255,255,0.05)', borderRadius: '4px' }}>
                    {loadingResults ? (
                      <Icon name="spinner" loading />
                    ) : expandedSearchResults.length === 0 ? (
                      <span style={{ color: '#999', fontStyle: 'italic' }}>No results for this search.</span>
                    ) : (
                      <>
                        <Header as="h5" style={{ marginBottom: '0.5em' }}>
                          {expandedSearchResults.length} result(s)
                        </Header>
                        <Table compact size="small" basic="very" striped>
                          <Table.Header>
                            <Table.Row>
                              <Table.HeaderCell>Username</Table.HeaderCell>
                              <Table.HeaderCell>Directory</Table.HeaderCell>
                              <Table.HeaderCell>Files</Table.HeaderCell>
                              <Table.HeaderCell>Size</Table.HeaderCell>
                            </Table.Row>
                          </Table.Header>
                          <Table.Body>
                            {expandedSearchResults.slice(0, 20).map((r, idx) => {
                              const dir = r.files?.[0]?.filename
                                ? r.files[0].filename.split('/').slice(0, -1).join('/')
                                : '';
                              const totalSize = r.files?.reduce((sum, f) => sum + (f.size || 0), 0) || 0;
                              const sizeStr = totalSize > 1073741824
                                ? `${(totalSize / 1073741824).toFixed(1)} GB`
                                : totalSize > 1048576
                                  ? `${(totalSize / 1048576).toFixed(1)} MB`
                                  : `${(totalSize / 1024).toFixed(0)} KB`;
                              return (
                                <Table.Row key={idx}>
                                  <Table.Cell>{r.username}</Table.Cell>
                                  <Table.Cell className="truncate-cell" title={dir}>{dir}</Table.Cell>
                                  <Table.Cell>{r.fileCount}</Table.Cell>
                                  <Table.Cell>{sizeStr}</Table.Cell>
                                </Table.Row>
                              );
                            })}
                          </Table.Body>
                        </Table>
                        {expandedSearchResults.length > 20 && (
                          <span style={{ color: '#999', fontSize: '0.85em' }}>
                            Showing first 20 of {expandedSearchResults.length} results.
                          </span>
                        )}
                      </>
                    )}
                  </div>
                )}
              </>
            )}
          </Table.Cell>
        </Table.Row>
      )}
    </>
  );
};

const WishlistItemCard = ({
  item,
  onDelete,
  onEdit,
  onMarkViewed,
  onRunSearch,
  selected,
  onSelect,
}) => {
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [running, setRunning] = useState(false);
  const [expanded, setExpanded] = useState(false);
  const [relatedSearches, setRelatedSearches] = useState([]);
  const [loadingSearches, setLoadingSearches] = useState(false);
  const [expandedSearchId, setExpandedSearchId] = useState(null);
  const [expandedSearchResults, setExpandedSearchResults] = useState([]);
  const [loadingResults, setLoadingResults] = useState(false);
  const requestState = getWishlistRequestState(item, []);
  const unseenCount = getUnseenCount(item);

  const handleRunSearch = async () => {
    setRunning(true);
    try {
      const result = await onRunSearch(item.id);
      toast.success(`Search completed with ${result.responseCount} results`);
    } catch (error) {
      toast.error(`Search failed: ${error.message}`);
    } finally {
      setRunning(false);
    }
  };

  const handleToggleExpand = async () => {
    const next = !expanded;
    setExpanded(next);
    if (next && relatedSearches.length === 0) {
      setLoadingSearches(true);
      try {
        const searches = await wishlistAPI.getSearches(item.id);
        setRelatedSearches(searches);
        if (unseenCount > 0) {
          await onMarkViewed(item.id);
        }
      } catch (error) {
        toast.error(`Failed to load searches: ${error.message}`);
      } finally {
        setLoadingSearches(false);
      }
    }
  };

  const handleMarkViewedClick = async () => {
    await onMarkViewed(item.id);
    toast.info(`Marked "${item.searchText}" as viewed`);
  };

  const handleToggleResults = async (searchId) => {
    if (expandedSearchId === searchId) {
      setExpandedSearchId(null);
      setExpandedSearchResults([]);
      return;
    }
    setExpandedSearchId(searchId);
    setLoadingResults(true);
    try {
      const results = await searchesAPI.getResponses({ id: searchId });
      setExpandedSearchResults(results);
    } catch (error) {
      toast.error(`Failed to load results: ${error.message}`);
      setExpandedSearchResults([]);
    } finally {
      setLoadingResults(false);
    }
  };

  return (
    <>
      <Segment
        className="wishlist-card"
        style={{
          borderLeft: selected ? '4px solid #2185d0' : '4px solid transparent',
          marginBottom: 0,
        }}
      >
        <div style={{ display: 'flex', alignItems: 'flex-start', gap: '0.75em' }}>
          <Checkbox
            checked={selected}
            onChange={(_, { checked }) => onSelect(item.id, checked)}
            style={{ marginTop: '0.3em' }}
          />
          <div style={{ flex: 1 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5em', marginBottom: '0.25em' }}>
              <Icon
                color={item.enabled ? 'green' : 'grey'}
                name={item.enabled ? 'check circle' : 'circle outline'}
              />
              <strong style={{ fontSize: '1.1em' }}>{item.searchText}</strong>
              {item.filter && (
                <Label basic color="blue" size="mini">
                  {item.filter}
                </Label>
              )}
              {unseenCount > 0 && (
                <Popup
                  content={`${unseenCount} new result(s) since you last viewed this item`}
                  trigger={
                    <Label color="red" size="mini">
                      {unseenCount} new
                    </Label>
                  }
                />
              )}
              <Popup
                content={requestState.summary}
                position="top center"
                trigger={
                  <Label color={requestState.color} size="mini">
                    {requestState.label}
                  </Label>
                }
              />
            </div>
            <div style={{ display: 'flex', gap: '1em', fontSize: '0.85em', color: '#888', flexWrap: 'wrap' }}>
              <span>Last run: {formatDate(item.lastSearchedAt)}</span>
              <span>Matches: {item.lastMatchCount}</span>
              <span>Runs: {item.totalSearchCount}</span>
              <span>
                Auto-download:{' '}
                <Icon name={item.autoDownload ? 'check' : 'close'} color={item.autoDownload ? 'green' : 'grey'} />
              </span>
              {item.autoDownload && item.maxDownloads && (
                <span>
                  Downloads: {item.totalDownloadCount}/{item.maxDownloads}
                </span>
              )}
            </div>
          </div>
          <div style={{ display: 'flex', gap: '0.25em' }}>
            <Popup
              content={expanded ? 'Collapse' : 'Expand to show search history and results'}
              position="top center"
              trigger={
                <Button
                  compact
                  icon={expanded ? 'angle up' : 'angle down'}
                  loading={loadingSearches}
                  onClick={handleToggleExpand}
                  size="mini"
                />
              }
            />
            {item.lastSearchId && (
              <Link
                onClick={() => unseenCount > 0 && onMarkViewed(item.id)}
                to={getSearchLink(item)}
              >
                <Popup
                  content={item.filter
                    ? 'View the latest search results with this wishlist filter already applied.'
                    : 'View the latest search results for this wishlist item.'}
                  position="top center"
                  trigger={
                    <Button
                      compact
                      icon="search"
                      size="mini"
                    />
                  }
                />
              </Link>
            )}
            {unseenCount > 0 && (
              <Popup
                content="Clear this item's new-results badge without opening its search history."
                position="top center"
                trigger={
                  <Button
                    compact
                    icon="check"
                    onClick={handleMarkViewedClick}
                    size="mini"
                  />
                }
              />
            )}
            <Popup
              content="Run search now"
              position="top center"
              trigger={
                <Button
                  compact
                  icon="play"
                  loading={running}
                  onClick={handleRunSearch}
                  primary
                  size="mini"
                />
              }
            />
            <Popup
              content="Edit"
              position="top center"
              trigger={
                <Button
                  compact
                  icon="edit"
                  onClick={() => onEdit(item)}
                  size="mini"
                />
              }
            />
            <Popup
              content="Delete"
              position="top center"
              trigger={
                <Button
                  color="red"
                  compact
                  icon="trash"
                  onClick={() => setConfirmDelete(true)}
                  size="mini"
                />
              }
            />
          </div>
        </div>

        {expanded && (
          <div style={{ marginTop: '1em', paddingTop: '1em', borderTop: '1px solid rgba(255,255,255,0.1)' }}>
            {loadingSearches ? (
              <Icon name="spinner" loading />
            ) : relatedSearches.length === 0 ? (
              <span style={{ color: '#999', fontStyle: 'italic' }}>
                No linked search history for this item yet.
                {item.lastSearchId && (
                  <>
                    {' '}
                    <Link
                      onClick={() => unseenCount > 0 && onMarkViewed(item.id)}
                      to={getSearchLink(item)}
                    >
                      Open latest search result.
                    </Link>
                  </>
                )}
              </span>
            ) : (
              <>
                <Table compact size="small" basic="very">
                  <Table.Header>
                    <Table.Row>
                      <Table.HeaderCell>Search</Table.HeaderCell>
                      <Table.HeaderCell>Source</Table.HeaderCell>
                      <Table.HeaderCell>Responses</Table.HeaderCell>
                      <Table.HeaderCell>Started</Table.HeaderCell>
                      <Table.HeaderCell />
                    </Table.Row>
                  </Table.Header>
                  <Table.Body>
                    {relatedSearches.map((s) => (
                      <Table.Row key={s.id}>
                        <Table.Cell>{s.searchText}</Table.Cell>
                        <Table.Cell>
                          <Label
                            color={s.source === 'wishlist' ? 'blue' : s.source === 'auto-replace' ? 'orange' : 'grey'}
                            size="mini"
                          >
                            {s.source || 'manual'}
                          </Label>
                        </Table.Cell>
                        <Table.Cell>{s.responseCount}</Table.Cell>
                        <Table.Cell>{formatDate(s.startedAt)}</Table.Cell>
                        <Table.Cell>
                          <Link
                            onClick={() => unseenCount > 0 && onMarkViewed(item.id)}
                            to={getSearchLink(item, s.id)}
                          >
                            <Popup
                              content={item.filter
                                ? 'View full search page with this wishlist filter already applied.'
                                : 'View full search page.'}
                              position="top center"
                              trigger={
                                <Button
                                  compact
                                  icon="external"
                                  size="mini"
                                />
                              }
                            />
                          </Link>
                          <Popup
                            content={expandedSearchId === s.id ? 'Hide results' : 'Show results inline'}
                            position="top center"
                            trigger={
                              <Button
                                compact
                                icon={expandedSearchId === s.id ? 'angle up' : 'angle down'}
                                loading={loadingResults && expandedSearchId === s.id}
                                onClick={() => handleToggleResults(s.id)}
                                size="mini"
                              />
                            }
                          />
                        </Table.Cell>
                      </Table.Row>
                    ))}
                  </Table.Body>
                </Table>
                {expandedSearchId && (
                  <div style={{ marginTop: '0.5em', padding: '0.5em', background: 'rgba(255,255,255,0.05)', borderRadius: '4px' }}>
                    {loadingResults ? (
                      <Icon name="spinner" loading />
                    ) : expandedSearchResults.length === 0 ? (
                      <span style={{ color: '#999', fontStyle: 'italic' }}>No results for this search.</span>
                    ) : (
                      <>
                        <Header as="h5" style={{ marginBottom: '0.5em' }}>
                          {expandedSearchResults.length} result(s)
                        </Header>
                        <Table compact size="small" basic="very" striped>
                          <Table.Header>
                            <Table.Row>
                              <Table.HeaderCell>Username</Table.HeaderCell>
                              <Table.HeaderCell>Directory</Table.HeaderCell>
                              <Table.HeaderCell>Files</Table.HeaderCell>
                              <Table.HeaderCell>Size</Table.HeaderCell>
                            </Table.Row>
                          </Table.Header>
                          <Table.Body>
                            {expandedSearchResults.slice(0, 20).map((r, idx) => {
                              const dir = r.files?.[0]?.filename
                                ? r.files[0].filename.split('/').slice(0, -1).join('/')
                                : '';
                              const totalSize = r.files?.reduce((sum, f) => sum + (f.size || 0), 0) || 0;
                              const sizeStr = totalSize > 1073741824
                                ? `${(totalSize / 1073741824).toFixed(1)} GB`
                                : totalSize > 1048576
                                  ? `${(totalSize / 1048576).toFixed(1)} MB`
                                  : `${(totalSize / 1024).toFixed(0)} KB`;
                              return (
                                <Table.Row key={idx}>
                                  <Table.Cell>{r.username}</Table.Cell>
                                  <Table.Cell className="truncate-cell" title={dir}>{dir}</Table.Cell>
                                  <Table.Cell>{r.fileCount}</Table.Cell>
                                  <Table.Cell>{sizeStr}</Table.Cell>
                                </Table.Row>
                              );
                            })}
                          </Table.Body>
                        </Table>
                        {expandedSearchResults.length > 20 && (
                          <span style={{ color: '#999', fontSize: '0.85em' }}>
                            Showing first 20 of {expandedSearchResults.length} results.
                          </span>
                        )}
                      </>
                    )}
                  </div>
                )}
              </>
            )}
          </div>
        )}
      </Segment>
      <Confirm
        cancelButton="Cancel"
        confirmButton="Delete"
        content={`Delete wishlist item "${item.searchText}"?`}
        header="Confirm Delete"
        onCancel={() => setConfirmDelete(false)}
        onConfirm={() => {
          setConfirmDelete(false);
          onDelete(item.id);
        }}
        open={confirmDelete}
        size="mini"
      />
    </>
  );
};

const FILTER_PRESETS = [
  { label: 'FLAC', value: 'flac' },
  { label: 'MP3', value: 'mp3' },
  { label: 'FLAC + MP3', value: 'flac OR mp3' },
  { label: 'FLAC + ALAC', value: 'flac OR alac' },
  { label: 'Lossless', value: 'flac OR alac OR wav OR ape' },
  { label: 'Any', value: '' },
];

const validateFilter = (filter) => {
  if (!filter || !filter.trim()) return null;
  const invalid = filter.match(/[^\w.\-\s]/);
  if (invalid) {
    return 'Filter may only contain words, extensions, exclusions prefixed with -, and OR';
  }
  return null;
};

const WishlistModal = ({ item, onClose, onSave }) => {
  const [searchText, setSearchText] = useState(item?.searchText || '');
  const [filter, setFilter] = useState(item?.filter || '');
  const [enabled, setEnabled] = useState(item?.enabled ?? true);
  const [autoDownload, setAutoDownload] = useState(item?.autoDownload ?? false);
  const [maxResults, setMaxResults] = useState(item?.maxResults ?? 100);
  const [maxDownloads, setMaxDownloads] = useState(item?.maxDownloads || '');
  const [saving, setSaving] = useState(false);
  const [filterError, setFilterError] = useState(null);

  const isEdit = Boolean(item?.id);

  const handleFilterChange = (value) => {
    setFilter(value);
    setFilterError(validateFilter(value));
  };

  const handleSave = async () => {
    if (!searchText.trim()) {
      toast.error('Search text is required');
      return;
    }
    if (filterError) {
      toast.error('Please fix the filter error before saving');
      return;
    }

    setSaving(true);
    try {
      await onSave({
        autoDownload,
        enabled,
        filter: filter.trim() || undefined,
        id: item?.id,
        maxDownloads: maxDownloads === '' ? null : Number.parseInt(maxDownloads, 10) || null,
        maxResults,
        searchText: searchText.trim(),
      });
      onClose();
    } catch (error) {
      toast.error(`Failed to save: ${error.message}`);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      onClose={onClose}
      open
      size="small"
    >
      <Modal.Header>
        <Icon name="star" />
        {isEdit ? 'Edit Wishlist Item' : 'Add to Wishlist'}
      </Modal.Header>
      <Modal.Content>
        <Form>
          <Form.Input
            label="Search Text"
            onChange={(event) => setSearchText(event.target.value)}
            placeholder="Enter search terms..."
            required
            value={searchText}
          />
          <Form.Field>
            <label>Filter (optional)</label>
            <Popup
              content="Only accept matching filenames or extensions, and exclude unwanted words with a leading dash."
              position="top center"
              trigger={
                <Form.Input
                  error={!!filterError}
                  onChange={(event) => handleFilterChange(event.target.value)}
                  placeholder="e.g., flac OR mp3"
                  value={filter}
                />
              }
            />
            {filterError && (
              <Label basic color="red" pointing>
                {filterError}
              </Label>
            )}
            <div style={{ marginTop: '0.5em' }}>
              {FILTER_PRESETS.map((preset) => (
                <Popup
                  key={preset.value}
                  content={
                    preset.value
                      ? `Filter to ${preset.label} files`
                      : 'Accept any file format'
                  }
                  trigger={
                    <Button
                      active={filter === preset.value}
                      compact
                      onClick={() => handleFilterChange(preset.value)}
                      size="mini"
                      style={{ marginRight: '0.25em', marginBottom: '0.25em' }}
                      toggle={!!preset.value}
                    >
                      {preset.label}
                    </Button>
                  }
                />
              ))}
            </div>
          </Form.Field>
          <Form.Input
            label="Max Results"
            max={1_000}
            min={10}
            onChange={(event) =>
              setMaxResults(Number.parseInt(event.target.value, 10) || 100)
            }
            type="number"
            value={maxResults}
          />
          <Form.Field>
            <Checkbox
              checked={enabled}
              label="Enabled (run automatically)"
              onChange={(_, data) => setEnabled(data.checked)}
              toggle
            />
          </Form.Field>
          <Form.Field>
            <Checkbox
              checked={autoDownload}
              label="Auto-download best matches"
              onChange={(_, data) => setAutoDownload(data.checked)}
              toggle
            />
          </Form.Field>
          <Form.Field>
            <Popup
              content="Automatically disable this item after N successful downloads. Leave blank to disable after the first download."
              position="top center"
              trigger={
                <label>
                  Auto-disable after downloads
                </label>
              }
            />
            <Form.Input
              label="Leave blank = disable after first download"
              min={1}
              onChange={(event) => setMaxDownloads(event.target.value)}
              placeholder="e.g., 5 for album parts"
              type="number"
              value={maxDownloads}
            />
          </Form.Field>
        </Form>
      </Modal.Content>
      <Modal.Actions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          loading={saving}
          onClick={handleSave}
          primary
        >
          {isEdit ? 'Save' : 'Add'}
        </Button>
      </Modal.Actions>
    </Modal>
  );
};

const CsvImportModal = ({ onClose, onImport }) => {
  const [csvText, setCsvText] = useState('');
  const [filter, setFilter] = useState('');
  const [enabled, setEnabled] = useState(true);
  const [autoDownload, setAutoDownload] = useState(false);
  const [includeAlbum, setIncludeAlbum] = useState(false);
  const [maxResults, setMaxResults] = useState(100);
  const [importing, setImporting] = useState(false);

  const handleFile = async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;
    setCsvText(await file.text());
  };

  const handleImport = async () => {
    if (!csvText.trim()) {
      toast.error('CSV text is required');
      return;
    }

    setImporting(true);
    try {
      await onImport({
        autoDownload,
        csvText,
        enabled,
        filter: filter.trim() || undefined,
        includeAlbum,
        maxResults,
      });
      onClose();
    } catch (error) {
      toast.error(`CSV import failed: ${error.message}`);
    } finally {
      setImporting(false);
    }
  };

  return (
    <Modal
      onClose={onClose}
      open
      size="small"
    >
      <Modal.Header>
        <Icon name="file alternate outline" />
        Import CSV Playlist
      </Modal.Header>
      <Modal.Content>
        <Form>
          <Form.Input
            accept=".csv,text/csv"
            label="CSV File"
            onChange={handleFile}
            type="file"
          />
          <Form.TextArea
            label="CSV Text"
            onChange={(event) => setCsvText(event.target.value)}
            placeholder="Track name,Artist name,Album name"
            rows={8}
            value={csvText}
          />
          <Form.Input
            label="Filter (optional)"
            onChange={(event) => setFilter(event.target.value)}
            placeholder="e.g., flac OR mp3"
            value={filter}
          />
          <Form.Input
            label="Max Results"
            max={1_000}
            min={1}
            onChange={(event) =>
              setMaxResults(Number.parseInt(event.target.value, 10) || 100)
            }
            type="number"
            value={maxResults}
          />
          <Form.Group widths="equal">
            <Form.Field>
              <Checkbox
                checked={enabled}
                label="Enabled"
                onChange={(_, data) => setEnabled(data.checked)}
                toggle
              />
            </Form.Field>
            <Form.Field>
              <Checkbox
                checked={autoDownload}
                label="Auto-download matches"
                onChange={(_, data) => setAutoDownload(data.checked)}
                toggle
              />
            </Form.Field>
            <Form.Field>
              <Checkbox
                checked={includeAlbum}
                label="Include album"
                onChange={(_, data) => setIncludeAlbum(data.checked)}
                toggle
              />
            </Form.Field>
          </Form.Group>
        </Form>
      </Modal.Content>
      <Modal.Actions>
        <Popup
          content="Close the CSV importer without adding any wishlist searches."
          trigger={<Button onClick={onClose}>Cancel</Button>}
        />
        <Popup
          content="Create wishlist searches from the parsed CSV rows using the selected options."
          trigger={
            <Button
              loading={importing}
              onClick={handleImport}
              primary
            >
              Import
            </Button>
          }
        />
      </Modal.Actions>
    </Modal>
  );
};

const Wishlist = () => {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [modalItem, setModalItem] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [showImportModal, setShowImportModal] = useState(false);
  const [requestCopyStatus, setRequestCopyStatus] = useState('');
  const [bulkRunning, setBulkRunning] = useState(false);
  const [viewMode, setViewMode] = useState('table'); // 'table' or 'cards'
  const [selectedIds, setSelectedIds] = useState(new Set());
  const requestSummary = useMemo(
    () =>
      buildWishlistRequestSummary({
        items,
      }),
    [items],
  );
  const runnableRequests = useMemo(
    () => getRunnableWishlistRequests(items, { limit: 3 }),
    [items],
  );

  const copyRequestReviewPacket = async () => {
    const packet = buildWishlistRequestReviewPacket({
      items,
    });
    const report = formatWishlistRequestReviewPacket(packet);

    if (!navigator.clipboard?.writeText) {
      setRequestCopyStatus('Clipboard unavailable; copy the request summary manually.');
      return;
    }

    try {
      await navigator.clipboard.writeText(report);
      setRequestCopyStatus('Wishlist request review copied.');
    } catch {
      setRequestCopyStatus('Unable to copy Wishlist request review.');
    }
  };

  const runEnabledSearches = async () => {
    setBulkRunning(true);
    const results = [];

    try {
      for (const item of runnableRequests) {
        try {
          const result = await wishlistAPI.runSearch(item.id);
          results.push({
            id: item.id,
            responseCount: result.responseCount ?? result.ResponseCount ?? 0,
            status: 'ran',
          });
        } catch (error) {
          results.push({
            error: error.message || 'Search failed',
            id: item.id,
            status: 'failed',
          });
        }
      }

      const ran = results.filter((result) => result.status === 'ran').length;
      const failed = results.filter((result) => result.status === 'failed').length;
      setRequestCopyStatus(
        `Ran ${ran} enabled Wishlist search${ran === 1 ? '' : 'es'}${
          failed ? `; ${failed} failed` : ''
        }. Downloads still require normal result selection and policy.`,
      );
      await loadItems();
    } finally {
      setBulkRunning(false);
    }
  };

  const [autoSearchEnabled, setAutoSearchEnabled] = useState(true);
  const [togglingAutoSearch, setTogglingAutoSearch] = useState(false);

  const loadOptions = useCallback(async () => {
    try {
      const opts = await optionsAPI.getCurrent();
      setAutoSearchEnabled(opts?.wishlist?.enabled ?? true);
    } catch {
      // leave optimistic default
    }
  }, []);

  const handleToggleAutoSearch = useCallback(async (_, { checked }) => {
    setTogglingAutoSearch(true);
    try {
      await optionsAPI.applyOverlay({ wishlist: { enabled: checked } });
      setAutoSearchEnabled(checked);
      toast.success(checked ? 'Auto-search enabled' : 'Auto-search paused');
    } catch (error) {
      toast.error(`Failed to update: ${error.message}`);
    } finally {
      setTogglingAutoSearch(false);
    }
  }, []);

  const loadItems = useCallback(async () => {
    try {
      const data = await wishlistAPI.getAll();
      setItems(Array.isArray(data) ? data : []);
    } catch (error) {
      toast.error(`Failed to load wishlist: ${error.message}`);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadOptions();
    loadItems();
  }, [loadOptions, loadItems]);

  const handleAdd = () => {
    setModalItem(null);
    setShowModal(true);
  };

  const handleImportClick = () => {
    setShowImportModal(true);
  };

  const handleEdit = (item) => {
    setModalItem(item);
    setShowModal(true);
  };

  const handleSave = async (item) => {
    if (item.id) {
      await wishlistAPI.update(item.id, item);
      toast.success('Wishlist item updated');
    } else {
      await wishlistAPI.create(item);
      toast.success('Added to wishlist');
    }

    await loadItems();
  };

  const handleDelete = async (id) => {
    try {
      await wishlistAPI.remove(id);
      toast.success('Wishlist item deleted');
      await loadItems();
    } catch (error) {
      toast.error(`Failed to delete: ${error.message}`);
    }
  };

  const handleRunSearch = async (id) => {
    const result = await wishlistAPI.runSearch(id);
    await loadItems();
    return result;
  };

  const handleMarkViewed = async (id) => {
    try {
      await wishlistAPI.markViewed(id);
      await loadItems();
    } catch (error) {
      // Silently ignore mark-viewed failures; they don't block UX
    }
  };

  // Bulk operations
  const handleSelectAll = (checked) => {
    if (checked) {
      setSelectedIds(new Set(items.map((i) => i.id)));
    } else {
      setSelectedIds(new Set());
    }
  };

  const handleSelectItem = (id, checked) => {
    const next = new Set(selectedIds);
    if (checked) {
      next.add(id);
    } else {
      next.delete(id);
    }
    setSelectedIds(next);
  };

  const handleBulkEnable = async () => {
    for (const id of selectedIds) {
      await wishlistAPI.update(id, { ...items.find((i) => i.id === id), enabled: true });
    }
    toast.success(`Enabled ${selectedIds.size} item(s)`);
    setSelectedIds(new Set());
    await loadItems();
  };

  const handleBulkDisable = async () => {
    for (const id of selectedIds) {
      await wishlistAPI.update(id, { ...items.find((i) => i.id === id), enabled: false });
    }
    toast.success(`Disabled ${selectedIds.size} item(s)`);
    setSelectedIds(new Set());
    await loadItems();
  };

  const handleBulkDelete = async () => {
    for (const id of selectedIds) {
      await wishlistAPI.remove(id);
    }
    toast.success(`Deleted ${selectedIds.size} item(s)`);
    setSelectedIds(new Set());
    await loadItems();
  };

  const handleImport = async (request) => {
    const result = await wishlistAPI.importCsv(request);
    toast.success(
      `Imported ${result.createdCount} searches (${result.duplicateCount} duplicates, ${result.skippedCount} skipped)`,
    );
    await loadItems();
  };

  return (
    <div className="wishlist-container">
      <Segment
        className="wishlist-header"
        clearing
      >
        <Header
          as="h2"
          floated="left"
        >
          <Icon name="star" />
          <Header.Content>
            Wishlist
            <Header.Subheader>
              Saved searches that run automatically
            </Header.Subheader>
          </Header.Content>
        </Header>
        <Popup
          content={autoSearchEnabled
            ? 'Auto-search is on — enabled items are searched automatically in the background.'
            : 'Auto-search is paused — items will not be searched until you turn it back on.'}
          trigger={
            <Checkbox
              checked={autoSearchEnabled}
              className="wishlist-autosearch-toggle"
              disabled={togglingAutoSearch}
              floated="right"
              label={autoSearchEnabled ? 'Auto-search on' : 'Auto-search off'}
              onChange={handleToggleAutoSearch}
              toggle
            />
          }
        />
        <Popup
          content="Add one saved search to the wishlist. Enabled wishlist entries run later using the normal conservative scheduler."
          trigger={
            <Button
              floated="right"
              icon
              labelPosition="left"
              onClick={handleAdd}
              primary
            >
              <Icon name="plus" />
              Add Search
            </Button>
          }
        />
        <Popup
          content="Import a playlist CSV, such as a TuneMyMusic export, into wishlist searches without starting a large search burst immediately."
          trigger={
            <Button
              floated="right"
              icon
              labelPosition="left"
              onClick={handleImportClick}
            >
              <Icon name="file alternate outline" />
              Import CSV
            </Button>
          }
        />
      </Segment>

      {!loading && (
        <Segment className="wishlist-request-summary">
          <div className="wishlist-request-summary-header">
            <Header as="h3">
              <Icon name="clipboard check" />
              Request Portal Summary
              <Header.Subheader>
                Operator view of wanted music before acquisition jobs are wired.
              </Header.Subheader>
            </Header>
            <Popup
              content="Copy the current Wishlist request review packet. This does not start searches, peer browsing, downloads, or automation."
              position="top center"
              trigger={
                <Button
                  aria-label="Copy Wishlist request review"
                  onClick={copyRequestReviewPacket}
                  size="small"
                >
                  <Icon name="copy" />
                  Copy Review
                </Button>
              }
            />
            <Popup
              content="Run up to three enabled Wishlist searches now through the backend. This starts search jobs only; downloads still require the normal result selection and policy."
              position="top center"
              trigger={
                <Button
                  aria-label="Run enabled Wishlist searches"
                  disabled={runnableRequests.length === 0}
                  loading={bulkRunning}
                  onClick={runEnabledSearches}
                  primary
                  size="small"
                >
                  <Icon name="play" />
                  Run Enabled
                </Button>
              }
            />
            <Popup
              content="Clear all unseen results badges across your wishlist. This marks every item as viewed."
              position="top center"
              trigger={
                <Button
                  aria-label="Mark all wishlist items as viewed"
                  icon="checkmark"
                  onClick={async () => {
                    try {
                      await wishlistAPI.markAllViewed();
                      await loadItems();
                      toast.success('All wishlist items marked as viewed');
                    } catch (error) {
                      toast.error(`Failed: ${error.message}`);
                    }
                  }}
                  size="small"
                >
                  Mark All Viewed
                </Button>
              }
            />
          </div>
          <div className="wishlist-request-summary-grid">
            <Label color="purple">
              Requests
              <Label.Detail>{requestSummary.total}</Label.Detail>
            </Label>
            <Label color="green">
              Enabled
              <Label.Detail>{requestSummary.enabled}</Label.Detail>
            </Label>
            <Label color="blue">
              Automatic
              <Label.Detail>{requestSummary.automatic}</Label.Detail>
            </Label>
            <Label color={requestSummary.reviewCount > 0 ? 'yellow' : 'grey'}>
              Needs Review
              <Label.Detail>{requestSummary.reviewCount}</Label.Detail>
            </Label>
            <Label color={requestSummary.quotaStatus === 'Within quota' ? 'green' : 'orange'}>
              {requestSummary.quotaStatus}
              <Label.Detail>{requestSummary.quotaRemaining} left</Label.Detail>
            </Label>
          </div>
          {requestCopyStatus && (
            <Label
              basic
              color="purple"
            >
              {requestCopyStatus}
            </Label>
          )}
        </Segment>
      )}

      {loading ? (
        <Segment
          loading
          placeholder
        />
      ) : items.length === 0 ? (
        <Segment
          inverted
          placeholder
        >
          <Header
            icon
            inverted
          >
            <Icon name="star outline" />
            No wishlist items yet
          </Header>
          <p>
            Add searches to your wishlist and they&apos;ll run automatically.
          </p>
          <Button
            onClick={handleAdd}
            primary
          >
            Add Your First Search
          </Button>
        </Segment>
      ) : (
        <>
          {selectedIds.size > 0 && (
            <Segment secondary compact style={{ marginBottom: '1em' }}>
              <span style={{ marginRight: '1em' }}>
                {selectedIds.size} item(s) selected
              </span>
              <Popup
                content="Enable all selected wishlist items"
                trigger={
                  <Button
                    compact
                    icon="play"
                    onClick={handleBulkEnable}
                    size="small"
                  />
                }
              />
              <Popup
                content="Disable all selected wishlist items"
                trigger={
                  <Button
                    compact
                    icon="pause"
                    onClick={handleBulkDisable}
                    size="small"
                  />
                }
              />
              <Popup
                content="Delete all selected wishlist items"
                trigger={
                  <Button
                    compact
                    color="red"
                    icon="trash"
                    onClick={handleBulkDelete}
                    size="small"
                  />
                }
              />
              <Button
                compact
                onClick={() => setSelectedIds(new Set())}
                size="small"
              >
                Clear
              </Button>
            </Segment>
          )}
          <div style={{ marginBottom: '0.75em', display: 'flex', gap: '0.5em', alignItems: 'center' }}>
            <Button.Group size="mini">
              <Popup
                content="Show wishlist as a table with columns"
                trigger={
                  <Button
                    active={viewMode === 'table'}
                    icon="table"
                    onClick={() => setViewMode('table')}
                  />
                }
              />
              <Popup
                content="Show wishlist as expandable cards with inline results"
                trigger={
                  <Button
                    active={viewMode === 'cards'}
                    icon="th"
                    onClick={() => setViewMode('cards')}
                  />
                }
              />
            </Button.Group>
          </div>
          {viewMode === 'table' ? (
            <Table
              celled
              striped
            >
              <Table.Header>
                <Table.Row>
                  <Table.HeaderCell width={1}>
                    <Popup
                      content="Select all items for bulk actions"
                      position="top center"
                      trigger={
                        <Checkbox
                          checked={selectedIds.size === items.length && items.length > 0}
                          indeterminate={selectedIds.size > 0 && selectedIds.size < items.length}
                          onChange={(_, { checked }) => handleSelectAll(checked)}
                        />
                      }
                    />
                  </Table.HeaderCell>
                  <Table.HeaderCell>Search</Table.HeaderCell>
                  <Table.HeaderCell
                    textAlign="center"
                    width={1}
                  >
                    Auto
                  </Table.HeaderCell>
                  <Table.HeaderCell width={3}>Last Run</Table.HeaderCell>
                  <Table.HeaderCell
                    textAlign="center"
                    width={1}
                  >
                    Matches
                  </Table.HeaderCell>
                  <Table.HeaderCell
                    textAlign="center"
                    width={1}
                  >
                    Runs
                  </Table.HeaderCell>
                  <Table.HeaderCell width={2}>Request State</Table.HeaderCell>
                  <Table.HeaderCell width={3}>Actions</Table.HeaderCell>
                </Table.Row>
              </Table.Header>
              <Table.Body>
                {items.map((item) => (
                  <WishlistItemRow
                    item={item}
                    key={item.id}
                    onDelete={handleDelete}
                    onEdit={handleEdit}
                    onMarkViewed={handleMarkViewed}
                    onRunSearch={handleRunSearch}
                    selected={selectedIds.has(item.id)}
                    onSelect={handleSelectItem}
                  />
                ))}
              </Table.Body>
            </Table>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75em' }}>
              {items.map((item) => (
                <WishlistItemCard
                  item={item}
                  key={item.id}
                  onDelete={handleDelete}
                  onEdit={handleEdit}
                  onMarkViewed={handleMarkViewed}
                  onRunSearch={handleRunSearch}
                  selected={selectedIds.has(item.id)}
                  onSelect={handleSelectItem}
                />
              ))}
            </div>
          )}
        </>
      )}

      {showModal && (
        <WishlistModal
          item={modalItem}
          onClose={() => setShowModal(false)}
          onSave={handleSave}
        />
      )}

      {showImportModal && (
        <CsvImportModal
          onClose={() => setShowImportModal(false)}
          onImport={handleImport}
        />
      )}
    </div>
  );
};

export default Wishlist;
