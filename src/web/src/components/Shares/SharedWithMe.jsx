import * as collectionsAPI from '../../lib/collections';
import * as identityAPI from '../../lib/identity';
import * as streaming from '../../lib/streaming';
import ErrorSegment from '../Shared/ErrorSegment';
import LoaderSegment from '../Shared/LoaderSegment';
import React, { Component } from 'react';
import { toast } from 'react-toastify';
import {
  Button,
  Container,
  Header,
  Icon,
  Label,
  Modal,
  Popup,
  Segment,
  Table,
} from 'semantic-ui-react';

const asArray = (value) => (Array.isArray(value) ? value : []);
const isObject = (value) => value && typeof value === 'object' && !Array.isArray(value);
const SHARE_PAGE_SIZE = 100;
const MANIFEST_ITEM_PAGE_SIZE = 100;

const getErrorMessage = (error, fallback) => {
  const data = error?.response?.data;

  if (typeof data === 'string') return data;
  if (isObject(data)) {
    return data.detail ||
      data.message ||
      data.error ||
      data.title ||
      JSON.stringify(data);
  }

  return error?.message || fallback;
};

const normalizeBackfillResult = (data) =>
  isObject(data)
    ? {
        enqueued: Number.isFinite(Number(data.enqueued)) ? Number(data.enqueued) : 0,
        failed: Number.isFinite(Number(data.failed)) ? Number(data.failed) : 0,
        message: typeof data.message === 'string' ? data.message : '',
      }
    : { enqueued: 0, failed: 0, message: '' };

export default class SharedWithMe extends Component {
  state = {
    backfilling: false,
    backfillResult: null,
    contacts: [],
    error: null,
    loading: true,
    manifest: null,
    manifestItemPage: 1,
    manifestLoading: false,
    manifestModalOpen: false,
    sharePage: 1,
    selectedShare: null,
    shares: [],
  };

  componentDidMount() {
    this.loadData();
  }

  loadData = async () => {
    try {
      this.setState({ error: null, loading: true });
      const [sharesRes, contactsRes] = await Promise.all([
        collectionsAPI.getShares().catch((error) => {
          // If 401/403/404/400, user isn't authenticated or feature not enabled - return empty list
          if (
            error.response?.status === 401 ||
            error.response?.status === 403 ||
            error.response?.status === 404 ||
            error.response?.status === 400
          ) {
            return { data: [] };
          }

          // For other errors, rethrow to be caught below
          throw error;
        }),
        identityAPI.getContacts().catch(() => ({ data: [] })), // Gracefully handle if Identity not enabled
      ]);

      const shares = asArray(sharesRes.data);

      this.setState({
        contacts: asArray(contactsRes.data),
        loading: false,
        sharePage: 1,
        shares,
      });
      this.loadCollectionDetails(shares);
    } catch (error) {
      // Only show error if it's not an auth/feature issue (which we handle above)
      const isAuthOrFeatureError =
        error.response?.status === 401 ||
        error.response?.status === 403 ||
        error.response?.status === 404 ||
        error.response?.status === 400;
      this.setState({
        error: isAuthOrFeatureError
          ? null
          : getErrorMessage(error, 'Failed to load incoming shares'),
        loading: false,
      });
    }
  };

  loadCollectionDetails = async (shares) => {
    const sharesWithCollections = await Promise.all(
      asArray(shares).map(async (share) => {
        try {
          const collectionRes = await collectionsAPI.getCollection(
            share.collectionId,
          );
          return { ...share, collection: collectionRes.data };
        } catch (error) {
          console.warn(
            'Failed to load collection for share',
            share.id,
            error,
          );
          return share;
        }
      }),
    );

    this.setState(({ shares: currentShares }) => {
      const currentIds = currentShares.map((share) => share.id).join('\n');
      const loadedIds = shares.map((share) => share.id).join('\n');

      if (currentIds !== loadedIds) {
        return null;
      }

      return { shares: sharesWithCollections };
    });
  };

  getContactNickname = (audienceId, audiencePeerId) => {
    if (audiencePeerId) {
      const contact = this.state.contacts.find(
        (c) => c.peerId === audiencePeerId,
      );
      return contact?.nickname || null;
    }

    // For legacy UserId, try to find by matching (this is a best-effort)
    return null;
  };

  getOwnerNickname = (collection) => {
    // Try to get from manifest if available
    if (collection?.ownerContactNickname) {
      return collection.ownerContactNickname;
    }

    // Try to find contact by ownerUserId (best effort)
    if (collection?.ownerUserId) {
      // For now, we can't reliably map UserId to PeerId without additional data
      // This would require storing PeerId in Collection or a lookup table
      return null;
    }

    return null;
  };

  handleViewManifest = async (share) => {
    try {
      this.setState({
        manifestLoading: true,
        manifestModalOpen: true,
        manifestItemPage: 1,
        selectedShare: share,
      });
      const manifestRes = await collectionsAPI.getShareManifest(share.id);
      this.setState({ manifest: manifestRes.data, manifestLoading: false });
    } catch (error) {
      this.setState({
        error: getErrorMessage(error, 'Failed to load manifest'),
        manifestLoading: false,
      });
    }
  };

  handleStreamItem = async (contentId, token) => {
    try {
      // Never put the long-lived share token in the URL. Exchange it (via header) for a short-lived,
      // content-bound stream ticket, then stream with that opaque ticket so no secret leaks into
      // browser history, proxy logs, or the server's access logs.
      if (token) {
        const ticket = await streaming.createShareStreamTicket(contentId, token);
        if (ticket) {
          window.open(streaming.buildTicketedStreamUrl(contentId, ticket), '_blank');
          return;
        }
      }

      window.open(streaming.buildDirectStreamUrl(contentId), '_blank');
    } catch (error) {
      this.setState({ error: getErrorMessage(error, 'Failed to start stream') });
    }
  };

  handleBackfill = async () => {
    const { selectedShare } = this.state;
    if (!selectedShare) return;

    try {
      this.setState({ backfilling: true, backfillResult: null, error: null });
      const result = await collectionsAPI.backfillShare(selectedShare.id);
      const backfillResult = normalizeBackfillResult(result.data);
      this.setState({
        backfilling: false,
        backfillResult,
      });

      if (backfillResult.failed === 0) {
        toast.success(backfillResult.message || 'Backfill started successfully');
      } else {
        toast.warning(
          backfillResult.message || 'Backfill started with some failures',
        );
      }
    } catch (error) {
      const errorMessage = getErrorMessage(error, 'Failed to start backfill');
      this.setState({
        backfilling: false,
        backfillResult: null,
        error: errorMessage,
      });
      toast.error(errorMessage);
    }
  };

  render() {
    const {
      error,
      loading,
      manifest,
      manifestItemPage,
      manifestLoading,
      manifestModalOpen,
      selectedShare,
      sharePage,
      shares,
    } = this.state;
    const sharePages = Math.max(1, Math.ceil(shares.length / SHARE_PAGE_SIZE));
    const currentSharePage = Math.min(sharePage, sharePages);
    const shareStart = (currentSharePage - 1) * SHARE_PAGE_SIZE;
    const visibleShares = shares.slice(shareStart, shareStart + SHARE_PAGE_SIZE);
    const sharePageStart = shares.length === 0 ? 0 : shareStart + 1;
    const sharePageEnd = Math.min(shareStart + SHARE_PAGE_SIZE, shares.length);
    const manifestItems = asArray(manifest?.items);
    const manifestItemPages = Math.max(
      1,
      Math.ceil(manifestItems.length / MANIFEST_ITEM_PAGE_SIZE),
    );
    const currentManifestItemPage = Math.min(manifestItemPage, manifestItemPages);
    const manifestItemStart = (currentManifestItemPage - 1) * MANIFEST_ITEM_PAGE_SIZE;
    const visibleManifestItems = manifestItems.slice(
      manifestItemStart,
      manifestItemStart + MANIFEST_ITEM_PAGE_SIZE,
    );
    const manifestItemPageStart = manifestItems.length === 0 ? 0 : manifestItemStart + 1;
    const manifestItemPageEnd = Math.min(
      manifestItemStart + MANIFEST_ITEM_PAGE_SIZE,
      manifestItems.length,
    );

    return (
      <Container>
        <Header as="h1">
          <Icon name="share" />
          <Header.Content>
            Shared with Me
            <Header.Subheader>Collections shared with you</Header.Subheader>
          </Header.Content>
        </Header>

        {error && <ErrorSegment caption={error} />}

        {shares.length === 0 ? (
          <Segment placeholder>
            <Header icon>
              <Icon name="inbox" />
              {loading ? 'Loading shares' : 'No shares yet'}
            </Header>
            {!loading && <p>Collections shared with you will appear here.</p>}
          </Segment>
        ) : (
          <Table>
            <Table.Header>
              <Table.Row>
                <Table.HeaderCell>Collection</Table.HeaderCell>
                <Table.HeaderCell>Shared By</Table.HeaderCell>
                <Table.HeaderCell>Type</Table.HeaderCell>
                <Table.HeaderCell>Permissions</Table.HeaderCell>
                <Table.HeaderCell>Actions</Table.HeaderCell>
              </Table.Row>
            </Table.Header>
            <Table.Body>
              {visibleShares.map((share) => {
                const ownerNickname = this.getOwnerNickname(share.collection);
                const displayName =
                  ownerNickname || share.collection?.ownerUserId || 'Unknown';

                return (
                  <Table.Row
                    data-testid={`incoming-share-row-${share.collection?.title || 'Untitled'}`}
                    key={share.id}
                  >
                    <Table.Cell>
                      <strong>{share.collection?.title || 'Untitled'}</strong>
                      {share.collection?.description && (
                        <div
                          style={{
                            color: '#666',
                            fontSize: '0.9em',
                            marginTop: '0.25em',
                          }}
                        >
                          {share.collection.description}
                        </div>
                      )}
                    </Table.Cell>
                    <Table.Cell>
                      {ownerNickname && (
                        <Label
                          color="blue"
                          style={{ marginRight: '0.5em' }}
                        >
                          {ownerNickname}
                        </Label>
                      )}
                      <span>{share.collection?.ownerUserId || 'Unknown'}</span>
                    </Table.Cell>
                    <Table.Cell>
                      {share.collection?.type || 'ShareList'}
                    </Table.Cell>
                    <Table.Cell>
                      {share.allowStream && <Label color="green">Stream</Label>}
                      {share.allowDownload && (
                        <Label color="blue">Download</Label>
                      )}
                      {share.allowReshare && <Label>Reshare</Label>}
                    </Table.Cell>
                    <Table.Cell>
                      <Button
                        data-testid="incoming-share-open"
                        onClick={() => this.handleViewManifest(share)}
                        primary
                        size="small"
                      >
                        View Contents
                      </Button>
                    </Table.Cell>
                  </Table.Row>
                );
              })}
            </Table.Body>
          </Table>
        )}
        {shares.length > SHARE_PAGE_SIZE && (
          <div className="shares-pagination">
            <span>
              {sharePageStart}–{sharePageEnd} of {shares.length}
            </span>
            <Popup
              content="Show the previous page of incoming shares."
              trigger={
                <Button
                  disabled={currentSharePage <= 1}
                  icon="chevron left"
                  onClick={() => this.setState({ sharePage: currentSharePage - 1 })}
                  size="mini"
                />
              }
            />
            <Popup
              content="Show the next page of incoming shares without rendering the whole list at once."
              trigger={
                <Button
                  disabled={currentSharePage >= sharePages}
                  icon="chevron right"
                  onClick={() => this.setState({ sharePage: currentSharePage + 1 })}
                  size="mini"
                />
              }
            />
          </div>
        )}

        {/* Manifest Modal */}
        <Modal
          onClose={() =>
            this.setState({
              manifest: null,
              manifestModalOpen: false,
              selectedShare: null,
            })
          }
          open={manifestModalOpen}
          size="large"
        >
          <Modal.Header>
            {selectedShare?.collection?.title ||
              manifest?.title ||
              'Collection Contents'}
            {manifest?.ownerContactNickname && (
              <span
                style={{
                  fontSize: '0.8em',
                  fontWeight: 'normal',
                  marginLeft: '1em',
                }}
              >
                by {manifest.ownerContactNickname}
              </span>
            )}
          </Modal.Header>
          <Modal.Content>
            {manifestLoading ? (
              <LoaderSegment />
            ) : manifest ? (
              <div data-testid="shared-manifest">
                {manifest.description && (
                  <p style={{ marginBottom: '1em' }}>{manifest.description}</p>
                )}
                {manifestItems.length > 0 ? (
                  <Table>
                    <Table.Header>
                      <Table.Row>
                        <Table.HeaderCell>Content ID</Table.HeaderCell>
                        <Table.HeaderCell>Media Kind</Table.HeaderCell>
                        <Table.HeaderCell>Actions</Table.HeaderCell>
                      </Table.Row>
                    </Table.Header>
                    <Table.Body>
                      {visibleManifestItems.map((item, index) => {
                        const itemIndex = manifestItemStart + index;
                        // Extract sha256 prefix from contentId (format: "sha256:...")
                        const sha256Prefix = item.contentId?.startsWith(
                          'sha256:',
                        )
                          ? item.contentId.slice(7, 15) // First 8 chars of hash
                          : item.contentId?.slice(0, 8) || `item-${itemIndex}`;
                        return (
                          <Table.Row
                            data-testid={`incoming-item-row-${sha256Prefix}`}
                            key={itemIndex}
                          >
                            <Table.Cell>
                              <code style={{ fontSize: '0.85em' }}>
                                {item.fileName ||
                                  item.contentId?.slice(0, 32) ||
                                  'Unknown'}
                              </code>
                            </Table.Cell>
                            <Table.Cell>
                              {item.mediaKind || 'Unknown'}
                            </Table.Cell>
                            <Table.Cell>
                              {item.streamUrl && (
                                <Button
                                  data-testid={`incoming-stream-${sha256Prefix}`}
                                  onClick={() => {
                                    const url = item.streamUrl.startsWith(
                                      'http',
                                    )
                                      ? item.streamUrl
                                      : `${window.location.origin}${item.streamUrl}`;
                                    window.open(url, '_blank');
                                  }}
                                  primary
                                  size="small"
                                >
                                  <Icon name="play" />
                                  Stream
                                </Button>
                              )}
                            </Table.Cell>
                          </Table.Row>
                        );
                      })}
                    </Table.Body>
                  </Table>
                ) : (
                  <Segment placeholder>
                    <Header icon>
                      <Icon name="file outline" />
                      No items in this collection
                    </Header>
                  </Segment>
                )}
                {manifestItems.length > MANIFEST_ITEM_PAGE_SIZE && (
                  <div className="shares-pagination">
                    <span>
                      {manifestItemPageStart}–{manifestItemPageEnd} of {manifestItems.length}
                    </span>
                    <Popup
                      content="Show the previous page of manifest items."
                      trigger={
                        <Button
                          disabled={currentManifestItemPage <= 1}
                          icon="chevron left"
                          onClick={() => this.setState({ manifestItemPage: currentManifestItemPage - 1 })}
                          size="mini"
                        />
                      }
                    />
                    <Popup
                      content="Show the next page of manifest items without rendering the whole manifest at once."
                      trigger={
                        <Button
                          disabled={currentManifestItemPage >= manifestItemPages}
                          icon="chevron right"
                          onClick={() => this.setState({ manifestItemPage: currentManifestItemPage + 1 })}
                          size="mini"
                        />
                      }
                    />
                  </div>
                )}
              </div>
            ) : (
              <ErrorSegment caption="Failed to load manifest" />
            )}
          </Modal.Content>
          <Modal.Actions>
            {selectedShare?.allowDownload && (
              <Button
                data-testid="incoming-backfill"
                disabled={this.state.backfilling}
                loading={this.state.backfilling}
                onClick={this.handleBackfill}
                primary
              >
                <Icon name="download" />
                Backfill All
              </Button>
            )}
            {this.state.backfillResult && (
              <span
                style={{ color: '#666', fontSize: '0.9em', marginRight: '1em' }}
              >
                {this.state.backfillResult.enqueued} enqueued,{' '}
                {this.state.backfillResult.failed} failed
              </span>
            )}
            <Button
              onClick={() =>
                this.setState({
                  backfillResult: null,
                  manifest: null,
                  manifestModalOpen: false,
                  selectedShare: null,
                })
              }
            >
              Close
            </Button>
          </Modal.Actions>
        </Modal>
      </Container>
    );
  }
}
