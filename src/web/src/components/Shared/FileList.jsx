import {
  formatAttributes,
  formatBytes,
  formatSeconds,
  getFileName,
} from '../../lib/util';
import React, { useMemo, useState } from 'react';
import TooltipButton from './TooltipButton';
import { Checkbox, Header, Icon, List, Table } from 'semantic-ui-react';

const FileList = ({
  directoryName,
  disabled,
  files,
  footer,
  locked,
  onClose,
  onSelectionChange,
}) => {
  const [folded, setFolded] = useState(false);
  const [lastSelectedIndex, setLastSelectedIndex] = useState(null);
  const sortedFiles = useMemo(
    () =>
      files
        ? [...files].sort((a, b) => (a.filename > b.filename ? 1 : -1))
        : [],
    [files],
  );
  const allSelected =
    sortedFiles.length > 0 && sortedFiles.every((file) => file.selected);
  const handleSelectionChange = (event, file, index, checked) => {
    if (
      event.shiftKey &&
      lastSelectedIndex !== null &&
      lastSelectedIndex !== index
    ) {
      const start = Math.min(lastSelectedIndex, index);
      const end = Math.max(lastSelectedIndex, index);
      sortedFiles
        .slice(start, end + 1)
        .forEach((rangeFile) => onSelectionChange(rangeFile, checked));
    } else {
      onSelectionChange(file, checked);
    }

    setLastSelectedIndex(index);
  };

  return (
    <div
      className="filelist"
      style={{ opacity: locked ? 0.5 : 1 }}
    >
      <Header
        className="filelist-header"
        size="small"
      >
        <div className="filelist-title">
          <TooltipButton
            aria-label={locked
              ? `${directoryName} is locked`
              : `${folded ? 'Expand' : 'Collapse'} ${directoryName}`}
            basic
            className="filelist-action-button"
            compact
            disabled={locked}
            icon
            onClick={() => setFolded(!folded)}
            tooltip={locked
              ? `${directoryName} is locked by the peer and cannot be expanded.`
              : folded
                ? `Show the files inside ${directoryName}.`
                : `Hide the files inside ${directoryName} while keeping this result open.`}
          >
            <Icon
              name={locked ? 'lock' : folded ? 'folder' : 'folder open'}
              size="large"
            />
          </TooltipButton>
          {directoryName}

          {Boolean(onClose) && (
            <TooltipButton
              aria-label={`Close ${directoryName} file list`}
              basic
              className="close-button"
              compact
              icon
              onClick={() => onClose()}
              tooltip={`Close the ${directoryName} file list and return to the surrounding search results.`}
            >
              <Icon color="red" name="close" />
            </TooltipButton>
          )}
        </div>
      </Header>
      {!folded && sortedFiles.length > 0 && (
        <List>
          <List.Item>
            <Table className="filelist-table">
              <Table.Header>
                <Table.Row>
                  <Table.HeaderCell className="filelist-selector">
                    <Checkbox
                      aria-label={`Select all files in ${directoryName}`}
                      checked={allSelected}
                      disabled={disabled}
                      fitted
                      onChange={(event, data) =>
                        sortedFiles.map((f) => onSelectionChange(f, data.checked))
                      }
                    />
                  </Table.HeaderCell>
                  <Table.HeaderCell className="filelist-filename">
                    File
                  </Table.HeaderCell>
                  <Table.HeaderCell className="filelist-size">
                    Size
                  </Table.HeaderCell>
                  <Table.HeaderCell className="filelist-attributes">
                    Attributes
                  </Table.HeaderCell>
                  <Table.HeaderCell className="filelist-length">
                    Length
                  </Table.HeaderCell>
                </Table.Row>
              </Table.Header>
              <Table.Body>
                {sortedFiles.map((f, index) => (
                  <Table.Row key={f.filename}>
                    <Table.Cell className="filelist-selector">
                      <Checkbox
                        aria-label={`Select ${f.filename}`}
                        checked={f.selected}
                        disabled={disabled}
                        fitted
                        onChange={(event, data) =>
                          handleSelectionChange(event, f, index, data.checked)
                        }
                      />
                    </Table.Cell>
                    <Table.Cell className="filelist-filename">
                      {locked ? <Icon name="lock" /> : ''}
                      {getFileName(f.filename)}
                    </Table.Cell>
                    <Table.Cell className="filelist-size">
                      {formatBytes(f.size)}
                    </Table.Cell>
                    <Table.Cell className="filelist-attributes">
                      {formatAttributes(f)}
                    </Table.Cell>
                    <Table.Cell className="filelist-length">
                      {formatSeconds(f.length)}
                    </Table.Cell>
                  </Table.Row>
                ))}
              </Table.Body>
              {footer && (
                <Table.Footer fullWidth>
                  <Table.Row>
                    <Table.HeaderCell colSpan="5">{footer}</Table.HeaderCell>
                  </Table.Row>
                </Table.Footer>
              )}
            </Table>
          </List.Item>
        </List>
      )}
    </div>
  );
};

export default FileList;
