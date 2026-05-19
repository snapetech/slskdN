import {
  formatTransferState,
  getFailureReason,
  isStateCancellable,
  isStateRemovable,
  isStateRetryable,
} from '../../lib/transfers';
import { transferKey } from '../../lib/transferStore';
import {
  formatBytes,
  formatBytesAsUnit,
  formatSeconds,
  getFileName,
} from '../../lib/util';
import React, { useCallback, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Checkbox, Icon, Popup } from 'semantic-ui-react';
import { FixedSizeList } from 'react-window';

const ROW_HEIGHT = 40;
const MAX_VIEWPORT_HEIGHT = 640;

const stateColor = (state) => {
  switch (state) {
    case 'InProgress':
      return 'blue';
    case 'Completed, Succeeded':
      return 'green';
    case 'Requested':
    case 'Queued':
    case 'Queued, Locally':
    case 'Queued, Remotely':
      return 'neutral';
    case 'Initializing':
      return 'teal';
    default:
      return 'red';
  }
};

const formatSize = ({ bytesTransferred, size }) => {
  const [s, unit] = formatBytes(size ?? 0, 1).split(' ');
  const t = formatBytesAsUnit(bytesTransferred ?? 0, unit, 1);
  return `${t} / ${s} ${unit}`;
};

const formatEta = (transfer) => {
  if (transfer.state !== 'InProgress') return '—';
  const ms = transfer.remainingTime;
  if (ms === null || ms === undefined || ms <= 0) return '—';
  return formatSeconds(Math.round(ms / 1_000));
};

const formatClock = (value) => {
  if (!value) return '';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '' : date.toLocaleTimeString();
};

const INTERACTIVE_SELECTOR = 'button, a, input, label, .ui.checkbox';

// react-window's inner scroll content; tag it as the grid's row group for AT.
const RowGroup = React.forwardRef(({ style, ...rest }, ref) => (
  <div
    ref={ref}
    role="rowgroup"
    style={style}
    {...rest}
  />
));

RowGroup.displayName = 'TransferTableRowGroup';

// grid-template-columns; keep in sync between header and rows.
const GRID_TEMPLATE =
  '36px minmax(220px, 2.4fr) minmax(110px, 1fr) 132px 188px 104px 84px 150px 104px';

const COLUMNS = [
  { key: 'filename', label: 'Name', sortable: true },
  { key: 'username', label: 'Peer', sortable: true },
  { key: 'size', label: 'Size', sortable: true },
  { key: 'progress', label: 'Progress', sortable: true },
  { key: 'averageSpeed', label: 'Speed', sortable: true },
  { key: 'eta', label: 'ETA', sortable: true },
  { key: 'state', label: 'State', sortable: true },
  { key: 'actions', label: '', sortable: false },
];

const sortValue = (transfer, key) => {
  switch (key) {
    case 'filename':
      return getFileName(transfer.filename ?? '').toLowerCase();
    case 'username':
      return (transfer.username ?? '').toLowerCase();
    case 'size':
      return transfer.size ?? 0;
    case 'progress':
      return transfer.percentComplete ?? 0;
    case 'averageSpeed':
      return transfer.state === 'InProgress' ? transfer.averageSpeed ?? 0 : -1;
    case 'eta':
      return transfer.state === 'InProgress'
        ? transfer.remainingTime ?? Number.MAX_SAFE_INTEGER
        : Number.MAX_SAFE_INTEGER;
    case 'state':
      return transfer.state ?? '';
    default:
      return 0;
  }
};

const Row = React.memo(({ data, index, style }) => {
  const {
    onCancel,
    onRemove,
    onRetry,
    onSelectionChange,
    selectedKeys,
    transfers,
  } = data;
  const transfer = transfers[index];
  const key = transferKey(transfer);
  const selected = selectedKeys.has(key);
  const inProgress = transfer.state === 'InProgress';
  const color = stateColor(transfer.state);
  const retryable = isStateRetryable(transfer.state);
  const cancellable = Boolean(isStateCancellable(transfer.state));
  const removable = isStateRemovable(transfer.state);
  const attempts = transfer.attempts ?? 1;
  const nextAttempt = formatClock(transfer.nextAttemptAt);

  const toggleSelection = () => onSelectionChange(transfer, !selected);

  const handleRowClick = (event) => {
    if (event.target.closest(INTERACTIVE_SELECTOR)) {
      return;
    }

    toggleSelection();
  };

  const handleRowKeyDown = (event) => {
    if (event.target !== event.currentTarget) {
      return;
    }

    if (event.key === ' ' || event.key === 'Enter') {
      event.preventDefault();
      toggleSelection();
    }
  };

  return (
    <div
      aria-selected={selected}
      className={`transfer-row ${index % 2 === 0 ? 'is-even' : 'is-odd'}${
        selected ? ' is-selected' : ''
      }`}
      onClick={handleRowClick}
      onKeyDown={handleRowKeyDown}
      role="row"
      style={{ ...style, gridTemplateColumns: GRID_TEMPLATE }}
      tabIndex={0}
    >
      <div
        className="transfer-cell transfer-cell-check"
        role="gridcell"
      >
        <Checkbox
          aria-label={`Select ${getFileName(transfer.filename)}`}
          checked={selected}
          fitted
          onChange={(_, d) => onSelectionChange(transfer, d.checked)}
        />
      </div>
      <div
        className="transfer-cell transfer-cell-name"
        role="gridcell"
        title={transfer.filename}
      >
        {getFileName(transfer.filename)}
      </div>
      <div
        className="transfer-cell transfer-cell-peer"
        role="gridcell"
        title={transfer.username}
      >
        <span className="transfer-peer-name">{transfer.username}</span>
        <Button
          aria-label={`Browse ${transfer.username}'s files`}
          as={Link}
          compact
          data-testid={`transfer-browse-user-${transfer.username}`}
          icon="folder open"
          size="mini"
          state={{ user: transfer.username }}
          title="Browse this user's files"
          to={`/browse?user=${encodeURIComponent(transfer.username)}`}
        />
      </div>
      <div
        className="transfer-cell transfer-cell-num"
        role="gridcell"
      >
        {formatSize(transfer)}
      </div>
      <div
        className="transfer-cell"
        role="gridcell"
      >
        {inProgress ? (
          <div className="transfer-progress">
            <div className="transfer-progress-track">
              <div
                className="transfer-progress-fill"
                style={{
                  width: `${Math.min(100, Math.round(transfer.percentComplete ?? 0))}%`,
                }}
              />
            </div>
            <span className="transfer-progress-label">
              {`${Math.round(transfer.percentComplete ?? 0)}%`}
            </span>
          </div>
        ) : (
          <Popup
            content={
              transfer.exception
                ? getFailureReason(transfer.exception) || transfer.exception
                : null
            }
            disabled={!transfer.exception}
            inverted
            position="top left"
            trigger={
              <span className={`transfer-state-pill transfer-state-${color}`}>
                {formatTransferState(transfer.state, transfer.exception)}
                {transfer.placeInQueue ? ` (#${transfer.placeInQueue})` : ''}
              </span>
            }
          />
        )}
      </div>
      <div
        className="transfer-cell transfer-cell-num"
        role="gridcell"
      >
        {inProgress && transfer.averageSpeed
          ? `${formatBytes(transfer.averageSpeed)}/s`
          : '—'}
      </div>
      <div
        className="transfer-cell transfer-cell-num"
        role="gridcell"
      >
        {formatEta(transfer)}
      </div>
      <div
        className="transfer-cell transfer-cell-state"
        role="gridcell"
      >
        <span className={`transfer-state-pill transfer-state-${color}`}>
          {formatTransferState(transfer.state, transfer.exception)}
        </span>
        {attempts > 1 && (
          <span
            className="transfer-retry-badge"
            title={`${attempts} attempts${
              nextAttempt ? ` · next around ${nextAttempt}` : ''
            }`}
          >
            <Icon name="redo" />
            {attempts}
          </span>
        )}
        {nextAttempt && !retryable && (
          <span
            className="transfer-next-attempt"
            title="Next retry around this time"
          >
            {`↻ ${nextAttempt}`}
          </span>
        )}
      </div>
      <div
        className="transfer-cell transfer-cell-actions"
        role="gridcell"
      >
        {retryable && (
          <Button
            color="green"
            icon="redo"
            onClick={() => onRetry(transfer)}
            size="mini"
            title="Retry"
          />
        )}
        {cancellable && (
          <Button
            color="red"
            icon="x"
            onClick={() => onCancel(transfer)}
            size="mini"
            title="Cancel"
          />
        )}
        {removable && (
          <Button
            icon="trash alternate"
            onClick={() => onRemove(transfer)}
            size="mini"
            title="Remove"
          />
        )}
      </div>
    </div>
  );
});

Row.displayName = 'TransferTableRow';

const TransferTable = ({
  onCancel,
  onCancelSelected,
  onRemove,
  onRemoveSelected,
  onRetry,
  onRetrySelected,
  onSelectAll,
  onSelectionChange,
  selectedFiles,
  selectedKeys,
  transfers,
}) => {
  const [sort, setSort] = useState({ direction: 'ascending', key: 'filename' });

  const sorted = useMemo(() => {
    const rows = [...transfers];
    rows.sort((a, b) => {
      const av = sortValue(a, sort.key);
      const bv = sortValue(b, sort.key);
      if (av < bv) return sort.direction === 'ascending' ? -1 : 1;
      if (av > bv) return sort.direction === 'ascending' ? 1 : -1;
      return 0;
    });
    return rows;
  }, [transfers, sort]);

  const toggleSort = useCallback((key) => {
    setSort((previous) =>
      previous.key === key
        ? {
            direction:
              previous.direction === 'ascending' ? 'descending' : 'ascending',
            key,
          }
        : { direction: 'ascending', key },
    );
  }, []);

  const allSelected =
    sorted.length > 0 &&
    sorted.every((transfer) => selectedKeys.has(transferKey(transfer)));

  const itemData = useMemo(
    () => ({
      onCancel,
      onRemove,
      onRetry,
      onSelectionChange,
      selectedKeys,
      transfers: sorted,
    }),
    [onCancel, onRemove, onRetry, onSelectionChange, selectedKeys, sorted],
  );

  const listHeight = Math.min(
    MAX_VIEWPORT_HEIGHT,
    Math.max(ROW_HEIGHT, sorted.length * ROW_HEIGHT),
  );

  const hasSelection = selectedFiles.length > 0;

  return (
    <div className="transfer-table-wrapper">
      {hasSelection && (
        <div className="transfer-bulk-bar">
          <span>{`${selectedFiles.length} selected`}</span>
          <Button.Group size="small">
            <Button
              color="green"
              content="Retry"
              icon="redo"
              onClick={onRetrySelected}
            />
            <Button
              color="red"
              content="Cancel"
              icon="x"
              onClick={onCancelSelected}
            />
            <Button
              content="Remove"
              icon="trash alternate"
              onClick={onRemoveSelected}
            />
          </Button.Group>
        </div>
      )}
      <div
        aria-label="Transfers"
        aria-rowcount={sorted.length}
        className="transfer-grid"
        role="grid"
      >
        <div
          className="transfer-row transfer-header-row"
          role="row"
          style={{ gridTemplateColumns: GRID_TEMPLATE }}
        >
          <div
            className="transfer-cell transfer-cell-check"
            role="columnheader"
          >
            <Checkbox
              aria-label="Select all transfers"
              checked={allSelected}
              fitted
              onChange={(_, d) => onSelectAll(sorted, d.checked)}
            />
          </div>
          {COLUMNS.map((column) => {
            const isSorted = sort.key === column.key;
            return (
              <div
                aria-sort={
                  column.sortable
                    ? isSorted
                      ? sort.direction
                      : 'none'
                    : undefined
                }
                className={`transfer-cell transfer-header-cell${
                  column.sortable ? ' is-sortable' : ''
                }`}
                key={column.key}
                onClick={
                  column.sortable ? () => toggleSort(column.key) : undefined
                }
                onKeyDown={
                  column.sortable
                    ? (event) => {
                        if (event.key === 'Enter' || event.key === ' ') {
                          event.preventDefault();
                          toggleSort(column.key);
                        }
                      }
                    : undefined
                }
                role="columnheader"
                tabIndex={column.sortable ? 0 : undefined}
              >
                {column.label}
                {isSorted && (
                  <Icon
                    name={
                      sort.direction === 'ascending'
                        ? 'caret up'
                        : 'caret down'
                    }
                  />
                )}
              </div>
            );
          })}
        </div>
        <FixedSizeList
          height={listHeight}
          innerElementType={RowGroup}
          itemCount={sorted.length}
          itemData={itemData}
          itemKey={(index) => transferKey(sorted[index])}
          itemSize={ROW_HEIGHT}
          width="100%"
        >
          {Row}
        </FixedSizeList>
      </div>
    </div>
  );
};

export default TransferTable;
