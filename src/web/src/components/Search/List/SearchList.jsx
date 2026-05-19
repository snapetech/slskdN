import ErrorSegment from '../../Shared/ErrorSegment';
import PlaceholderSegment from '../../Shared/PlaceholderSegment';
import SearchListRow from './SearchListRow';
import React from 'react';
import { Button, Card, Icon, Loader, Popup, Table } from 'semantic-ui-react';

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
  const filteredSearches = sourceFilter === 'all'
    ? searches
    : Object.fromEntries(
        Object.entries(searches).filter(
          ([, search]) => (search.source || 'manual').toLowerCase() === sourceFilter.toLowerCase(),
        ),
      );

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
            {Object.keys(filteredSearches).length} / {Object.keys(searches).length} searches
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
        ) : Object.keys(filteredSearches).length === 0 && !connecting ? (
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
                {Object.values(filteredSearches)
                  .sort((a, b) => new Date(b.startedAt) - new Date(a.startedAt))
                  .map((search) => (
                    <SearchListRow
                      key={search.id}
                      onRemove={onRemove}
                      onStop={onStop}
                      search={search}
                    />
                  ))}
              </Table.Body>
            </Table>
          </div>
        )}
      </Card.Content>
    </Card>
  );
};

export default SearchList;
