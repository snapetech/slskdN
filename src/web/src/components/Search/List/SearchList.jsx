import ErrorSegment from '../../Shared/ErrorSegment';
import PlaceholderSegment from '../../Shared/PlaceholderSegment';
import SearchListRow from './SearchListRow';
import React, { useEffect, useMemo, useState } from 'react';
import { Button, Card, Icon, Loader, Popup, Table } from 'semantic-ui-react';

const SEARCH_LIST_PAGE_SIZE = 100;

const SearchList = ({
  connecting = false,
  error = undefined,
  onCleanup = () => {},
  onRemove = () => {},
  onRemoveAll = () => {},
  onStop = () => {},
  removingAll = false,
  cleaningUp = false,
  searches = {},
  sourceFilter = 'all',
}) => {
  const [page, setPage] = useState(1);
  const filteredSearchValues = useMemo(() => {
    const values = Object.values(searches);
    const filtered = sourceFilter === 'all'
      ? values
      : values.filter(
          (search) => (search.source || 'manual').toLowerCase() === sourceFilter.toLowerCase(),
        );

    return filtered.sort((a, b) => new Date(b.startedAt) - new Date(a.startedAt));
  }, [searches, sourceFilter]);

  useEffect(() => {
    setPage(1);
  }, [sourceFilter]);

  const searchCount = Object.keys(searches).length;
  const filteredCount = filteredSearchValues.length;
  const totalPages = Math.max(1, Math.ceil(filteredCount / SEARCH_LIST_PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const start = (currentPage - 1) * SEARCH_LIST_PAGE_SIZE;
  const pageSearches = filteredSearchValues.slice(start, start + SEARCH_LIST_PAGE_SIZE);
  const pageStart = filteredCount === 0 ? 0 : start + 1;
  const pageEnd = Math.min(start + SEARCH_LIST_PAGE_SIZE, filteredCount);

  return (
    <Card
      className="search-list-card"
      raised
    >
      <Card.Content>
        <div className="search-list-header">
          <div style={{ display: 'flex', gap: '0.5em', flexWrap: 'wrap', alignItems: 'center' }}>
            <Popup
              content="Clear all completed searches"
              position="top center"
              trigger={
                <Button
                  color="red"
                  compact
                  disabled={removingAll || Object.keys(searches).length === 0}
                  icon
                  labelPosition="left"
                  loading={removingAll}
                  onClick={onRemoveAll}
                  size="small"
                >
                  <Icon name="trash" />
                  Clear All
                </Button>
              }
            />
            <Popup
              content="Clear old searches"
              position="top center"
              trigger={
                <Button
                  color="orange"
                  compact
                  disabled={cleaningUp || Object.keys(searches).length === 0}
                  icon
                  labelPosition="left"
                  loading={cleaningUp}
                  onClick={onCleanup}
                  size="small"
                >
                  <Icon name="clock outline" />
                  Clear Old
                </Button>
              }
            />
          </div>
          <div className="search-list-count">
            {filteredCount} / {searchCount} searches
          </div>
        </div>
        {connecting && (
          <Loader
            active
            inline="centered"
            size="small"
          />
        )}
        {error ? (
          <ErrorSegment caption={error} />
        ) : filteredCount === 0 && !connecting ? (
          <PlaceholderSegment
            caption={
              sourceFilter !== 'all'
                ? `No ${sourceFilter} searches to display`
                : 'No searches to display'
            }
            icon="search"
          />
        ) : (
          <div className="search-list-wrapper">
            <Table
              className="unstackable"
              size="large"
            >
              <Table.Header>
                <Table.Row>
                  <Table.HeaderCell className="search-list-action">
                    <Icon name="info circle" />
                  </Table.HeaderCell>
                  <Table.HeaderCell className="search-list-phrase">
                    Search
                  </Table.HeaderCell>
                  <Table.HeaderCell className="search-list-files">
                    Files
                  </Table.HeaderCell>
                  <Table.HeaderCell className="search-list-locked">
                    Locked
                  </Table.HeaderCell>
                  <Table.HeaderCell className="search-list-responses">
                    Responses
                  </Table.HeaderCell>
                  <Table.HeaderCell className="search-list-started">
                    Ended
                  </Table.HeaderCell>
                  <Table.HeaderCell className="search-list-action" />
                </Table.Row>
              </Table.Header>
              <Table.Body>
                {pageSearches.map((search) => (
                  <SearchListRow
                    key={search.id}
                    onRemove={onRemove}
                    onStop={onStop}
                    search={search}
                  />
                ))}
              </Table.Body>
            </Table>
            {filteredCount > SEARCH_LIST_PAGE_SIZE && (
              <div className="search-list-pagination">
                <span>
                  {pageStart}–{pageEnd} of {filteredCount}
                </span>
                <Button
                  disabled={currentPage <= 1}
                  icon="chevron left"
                  onClick={() => setPage(currentPage - 1)}
                  size="mini"
                />
                <Button
                  disabled={currentPage >= totalPages}
                  icon="chevron right"
                  onClick={() => setPage(currentPage + 1)}
                  size="mini"
                />
              </div>
            )}
          </div>
        )}
      </Card.Content>
    </Card>
  );
};

export default SearchList;
