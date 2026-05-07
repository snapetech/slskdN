import React, { memo, useCallback, useMemo, useState } from 'react';
import { Button, Icon, List } from 'semantic-ui-react';

const MAX_VISIBLE_FOLDERS = 2000;

const flattenVisibleDirectories = (nodes, expandedPaths, level = 0, rows = []) => {
  for (const directory of nodes) {
    rows.push({ directory, level });

    if (rows.length >= MAX_VISIBLE_FOLDERS) {
      return rows;
    }

    if (expandedPaths.has(directory.name) && directory.children?.length > 0) {
      flattenVisibleDirectories(directory.children, expandedPaths, level + 1, rows);
    }

    if (rows.length >= MAX_VISIBLE_FOLDERS) {
      return rows;
    }
  }

  return rows;
};

const DirectoryRow = memo(
  ({
    directory,
    expandedPaths,
    level,
    onDownload,
    onToggleExpand,
    onSelect,
    selectedDirectoryName,
  }) => {
    const isExpanded = expandedPaths.has(directory.name);
    const isActive = directory.name === selectedDirectoryName;
    const hasChildren = directory.children?.length > 0;
    const folderName = directory.name.split('\\').pop().split('/').pop();

    return (
      <List.Item style={{ paddingLeft: level > 0 ? `${level}em` : 0 }}>
        <List.Content>
          <div style={{ alignItems: 'center', display: 'flex', gap: '4px' }}>
            {hasChildren ? (
              <Icon
                name={isExpanded ? 'caret down' : 'caret right'}
                onClick={() => onToggleExpand(directory.name)}
                style={{ cursor: 'pointer', width: '16px' }}
              />
            ) : (
              <span style={{ width: '16px' }} />
            )}

            <Icon
              className={directory.locked ? 'locked' : ''}
              name={
                directory.locked ? 'lock' : isExpanded ? 'folder open' : 'folder'
              }
              style={{ opacity: directory.locked ? 0.5 : 1 }}
            />

            <span
              onClick={() => onSelect(directory)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  onSelect(directory);
                }
              }}
              role="button"
              style={{
                color: isActive ? '#2185d0' : 'inherit',
                cursor: 'pointer',
                fontWeight: isActive ? 'bold' : 'normal',
                opacity: directory.locked ? 0.5 : 1,
              }}
              tabIndex={0}
            >
              {folderName}
            </span>

            {level > 0 && (
              <Button
                basic
                compact
                icon="download"
                onClick={() => onDownload(directory)}
                size="mini"
                title={`Download ${folderName}`}
              />
            )}

            {directory.fileCount > 0 && (
              <span
                style={{
                  background: '#555',
                  borderRadius: '10px',
                  color: '#fff',
                  fontSize: '0.75em',
                  marginLeft: '6px',
                  padding: '1px 6px',
                }}
              >
                {directory.fileCount}
              </span>
            )}
          </div>
        </List.Content>
      </List.Item>
    );
  },
);

DirectoryRow.displayName = 'DirectoryRow';

const DirectoryTree = ({ onDownload, onSelect, selectedDirectoryName, tree }) => {
  const [expandedPaths, setExpandedPaths] = useState(new Set());

  const visibleDirectories = useMemo(
    () => flattenVisibleDirectories(tree, expandedPaths),
    [expandedPaths, tree],
  );

  const visibleLimitReached = visibleDirectories.length >= MAX_VISIBLE_FOLDERS;

  const toggleExpand = useCallback((path) => {
    setExpandedPaths((previous) => {
      const updated = new Set(previous);

      if (updated.has(path)) {
        updated.delete(path);
      } else {
        updated.add(path);
      }

      return updated;
    });
  }, []);

  const selectDirectory = useCallback(
    (directory) => {
      onSelect(null, directory);
    },
    [onSelect],
  );

  const collapseAll = useCallback(() => {
    setExpandedPaths(new Set());
  }, []);

  return (
    <div>
      <div
        style={{
          alignItems: 'center',
          borderBottom: '1px solid #333',
          display: 'flex',
          gap: '8px',
          marginBottom: '8px',
          paddingBottom: '8px',
        }}
      >
        <Button compact onClick={collapseAll} size="tiny">
          <Icon name="compress" /> Collapse All
        </Button>
      </div>

      <List className="browse-folderlist-list">
        {visibleDirectories.map(({ directory, level }) => (
          <DirectoryRow
            directory={directory}
            expandedPaths={expandedPaths}
            key={directory.name}
            level={level}
            onDownload={onDownload}
            onSelect={selectDirectory}
            onToggleExpand={toggleExpand}
            selectedDirectoryName={selectedDirectoryName}
          />
        ))}
      </List>

      {visibleLimitReached && (
        <div className="browse-folderlist-limit">
          Showing the first {MAX_VISIBLE_FOLDERS} visible folders. Collapse a branch
          to keep browsing deeper branches without locking the UI.
        </div>
      )}
    </div>
  );
};

export default DirectoryTree;
