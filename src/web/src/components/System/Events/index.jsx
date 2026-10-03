import '../System.css';
import { list } from '../../../lib/events';
import React, { useEffect, useState } from 'react';
import { Button, Header, Icon, Message, Pagination, Popup, Table } from 'semantic-ui-react';

const PER_PAGE = 10;

const replaceHyphensWithNonBreakingEquivalent = (string) =>
  string?.replaceAll('-', '‑');

const formatEventData = (data) => {
  try {
    return JSON.stringify(JSON.parse(data), null, 2);
  } catch {
    return `${data || ''}`;
  }
};

const Events = () => {
  const [page, setPage] = useState(1);
  const [retrySequence, setRetrySequence] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [events, setEvents] = useState([]);

  useEffect(() => {
    let active = true;

    const loadEvents = async () => {
      setLoading(true);
      setError(null);

      try {
        const response = await list({
          limit: PER_PAGE,
          offset: (page - 1) * PER_PAGE,
        });
        if (!active) return;

        const items = Array.isArray(response?.events) ? response.events : [];
        const totalCount = Number(response?.totalCount);
        const nextTotalPages = Number.isFinite(totalCount)
          ? Math.ceil(totalCount / PER_PAGE)
          : 0;

        setEvents(items);
        setTotalPages(nextTotalPages);
        if (nextTotalPages > 0 && page > nextTotalPages) {
          setPage(nextTotalPages);
        }
      } catch (error_) {
        if (!active) return;
        setEvents([]);
        setError(
          error_?.response?.data?.message ||
            error_?.message ||
            'Unable to load system events.',
        );
      } finally {
        if (active) setLoading(false);
      }
    };

    loadEvents();
    return () => {
      active = false;
    };
  }, [page, retrySequence]);

  const changePage = (_event, { activePage }) => {
    const nextPage = Number(activePage);
    if (Number.isInteger(nextPage) && nextPage >= 1) {
      setPage(nextPage);
    }
  };

  if (error) {
    return (
      <div className="events-error">
        <Message
          error
          header="Failed to load events"
          content={error}
        />
        <Popup
          content="Try loading this page of system events again after the current request error."
          position="top center"
          trigger={(
            <Button
              onClick={() => setRetrySequence((previous) => previous + 1)}
              primary
            >
              <Icon name="refresh" />
              Try Again
            </Button>
          )}
        />
      </div>
    );
  }

  return (
    <div className="events-page">
      <Header as="h2">
        <Icon name="history" />
        System Events
        <Header.Subheader>
          Review recorded events and their diagnostic details.
        </Header.Subheader>
      </Header>

      <div className="events-controls">
        {totalPages > 1 && (
          <Popup
            content="Choose a page to review another batch of system events."
            on={['hover', 'focus']}
            position="top center"
            trigger={(
              <Pagination
                activePage={page}
                aria-label="Event pages"
                boundaryRange={1}
                className="events-pagination"
                firstItem={{ 'aria-label': 'First events page', content: '«' }}
                lastItem={{ 'aria-label': 'Last events page', content: '»' }}
                nextItem={{ 'aria-label': 'Next events page', content: '›' }}
                onPageChange={changePage}
                prevItem={{ 'aria-label': 'Previous events page', content: '‹' }}
                siblingRange={1}
                totalPages={totalPages}
              />
            )}
          />
        )}
        {totalPages > 0 && (
          <span
            aria-live="polite"
            className="events-page-status"
          >
            Page {page} of {totalPages}
          </span>
        )}
      </div>

      <div
        aria-label="System event records"
        className="events-table-scroll"
        role="region"
        tabIndex={0}
      >
        <Table
          aria-label="System events"
          className="events-table"
          compact="very"
          unstackable
        >
          <Table.Header>
            <Table.Row>
              <Table.HeaderCell className="events-list-id">Id</Table.HeaderCell>
              <Table.HeaderCell className="events-list-timestamp">
                Timestamp
              </Table.HeaderCell>
              <Table.HeaderCell className="events-list-type">
                Type
              </Table.HeaderCell>
              <Table.HeaderCell>Data</Table.HeaderCell>
            </Table.Row>
          </Table.Header>
          <Table.Body className="events-table-body">
            {events.length === 0 ? (
              <Table.Row>
                <Table.Cell colSpan={4}>
                  {loading ? 'Loading events' : 'No events'}
                </Table.Cell>
              </Table.Row>
            ) : (
              events.map((event) => (
                <Table.Row key={event.id}>
                  <Table.Cell className="events-list-id">
                    <Popup
                      content={`Use this ID to find the ${event.type} event in diagnostics: ${event.id}`}
                      on={['hover', 'focus']}
                      position="top left"
                      trigger={(
                        <Button
                          aria-label={`Show identifier for event ${event.id}`}
                          basic
                          icon="info circle"
                          size="small"
                        />
                      )}
                      wide="very"
                    />
                  </Table.Cell>
                  <Table.Cell className="events-list-timestamp">
                    {replaceHyphensWithNonBreakingEquivalent(event.timestamp)}
                  </Table.Cell>
                  <Table.Cell className="events-list-type">{event.type}</Table.Cell>
                  <Table.Cell className="events-table-data">
                    <pre>{formatEventData(event.data)}</pre>
                  </Table.Cell>
                </Table.Row>
              ))
            )}
          </Table.Body>
        </Table>
      </div>
    </div>
  );
};

export default Events;
