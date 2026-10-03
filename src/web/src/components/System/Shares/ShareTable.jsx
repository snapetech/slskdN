import { Switch } from '../../Shared';
import React from 'react';
import { Button, Icon, Popup, Table } from 'semantic-ui-react';

const ShareTable = ({ onClick, shares }) => {
  return (
    <div
      aria-label="Configured shares"
      className="system-shares-table-scroll"
      role="region"
      tabIndex={0}
    >
      <Table>
        <Table.Header>
          <Table.Row>
            <Table.HeaderCell>Host</Table.HeaderCell>
            <Table.HeaderCell>Local Path</Table.HeaderCell>
            <Table.HeaderCell className="share-count-column">
              Directories
            </Table.HeaderCell>
            <Table.HeaderCell className="share-count-column">
              Files
            </Table.HeaderCell>
            <Table.HeaderCell>Alias</Table.HeaderCell>
            <Table.HeaderCell>Remote Path</Table.HeaderCell>
          </Table.Row>
        </Table.Header>
        <Table.Body>
          <Switch
            empty={
              shares.length === 0 && (
                <Table.Row>
                  <Table.Cell
                    colSpan={6}
                    style={{
                      opacity: 0.5,
                      padding: '10px !important',
                      textAlign: 'center',
                    }}
                  >
                    No shares configured
                  </Table.Cell>
                </Table.Row>
              )
            }
          >
            {shares.map((share) => {
              const label = `View files in share ${share.localPath}`;
              const description = `Open the file list for share ${share.localPath}.`;

              return (
                <Table.Row key={`${share.host}+${share.localPath}`}>
                  <Table.Cell>{share.host}</Table.Cell>
                  <Table.Cell>
                    <Popup
                      content={description}
                      position="top left"
                      trigger={(
                        <Button
                          aria-label={label}
                          basic
                          className="share-path-button"
                          onClick={() => onClick(share)}
                          title={description}
                        >
                          <Icon name="folder" />
                          {share.localPath}
                        </Button>
                      )}
                    />
                  </Table.Cell>
                  <Table.Cell>{share.directories ?? '?'}</Table.Cell>
                  <Table.Cell>{share.files ?? '?'}</Table.Cell>
                  <Table.Cell>{share.alias}</Table.Cell>
                  <Table.Cell>{share.remotePath}</Table.Cell>
                </Table.Row>
              );
            })}
          </Switch>
        </Table.Body>
      </Table>
    </div>
  );
};

export default ShareTable;
