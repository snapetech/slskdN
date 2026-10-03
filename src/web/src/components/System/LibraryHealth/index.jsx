import * as libraryHealth from '../../../lib/libraryHealth';
import {
  buildLibraryHealthActionPlan,
  buildLibraryHealthQuarantinePacket,
  buildLibraryHealthReport,
  buildLibraryHealthSafeFixManifest,
  buildLibraryHealthSearchSeeds,
  getLibraryHealthReplacementSearchQueries,
  getLibraryHealthSafeFixIssueIds,
} from '../../../lib/libraryHealthReport';
import { LoaderSegment } from '../../Shared';
import * as searches from '../../../lib/searches';
import './LibraryHealth.css';
import React, { useEffect, useRef, useState } from 'react';
import {
  Button,
  Grid,
  Header,
  Icon,
  Input,
  Label,
  Loader,
  Message,
  Popup,
  Segment,
  Statistic,
  Tab,
  Table,
} from 'semantic-ui-react';

const asArray = (value) => (Array.isArray(value) ? value : []);
const isObject = (value) => value && typeof value === 'object' && !Array.isArray(value);
const SCAN_POLL_INTERVAL_MS = 2_000;
const SCAN_POLL_TIMEOUT_MS = 60_000;

const LibraryHealth = () => {
  const [activeIndex, setActiveIndex] = useState(0);
  const [libraryPath, setLibraryPath] = useState('');
  const [scanning, setScanning] = useState(false);
  const [summary, setSummary] = useState(null);
  const [dashboardPath, setDashboardPath] = useState('');
  const [dashboardLoadFailed, setDashboardLoadFailed] = useState(false);
  const [issuesByType, setIssuesByType] = useState([]);
  const [issuesByArtist, setIssuesByArtist] = useState([]);
  const [issues, setIssues] = useState([]);
  const [selectedIssues, setSelectedIssues] = useState(new Set());
  const [fixing, setFixing] = useState(false);
  const [searchingReplacements, setSearchingReplacements] = useState(false);
  const [reportMessage, setReportMessage] = useState('');
  const [copyFallback, setCopyFallback] = useState('');
  const [copyFallbackLabel, setCopyFallbackLabel] = useState('');
  const [scanMonitoringPaused, setScanMonitoringPaused] = useState(false);
  const [scanMessage, setScanMessage] = useState('');
  const [scanMessageType, setScanMessageType] = useState('info');
  const [scanProgress, setScanProgress] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const dashboardRequestRef = useRef(0);
  const activeScanRef = useRef(null);
  const mountedRef = useRef(true);
  const scanMonitoringPausedRef = useRef(false);
  const pollDeadlineRef = useRef(null);
  const pollRequestRef = useRef(null);
  const pollScanStatusRef = useRef(null);
  const pollTimerRef = useRef(null);

  const clearScanTimer = () => {
    if (pollTimerRef.current) {
      clearTimeout(pollTimerRef.current);
      pollTimerRef.current = null;
    }
  };

  const resetScanPolling = () => {
    clearScanTimer();
    activeScanRef.current = null;
    pollDeadlineRef.current = null;
  };

  useEffect(() => {
    mountedRef.current = true;
    const handleVisibilityChange = () => {
      clearScanTimer();
      if (!document.hidden) {
        pollScanStatusRef.current?.();
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => {
      mountedRef.current = false;
      resetScanPolling();
      dashboardRequestRef.current += 1;
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, []);

  const loadSummary = async (path, { preserveFeedback = false } = {}) => {
    if (!path?.trim()) return;

    const requestId = dashboardRequestRef.current + 1;
    dashboardRequestRef.current = requestId;

    try {
      if (!mountedRef.current) {
        return;
      }

      setLoading(true);
      setError(null);
      setDashboardLoadFailed(false);
      const response = await libraryHealth.getDashboard(path, 10, 100);

      if (!mountedRef.current || dashboardRequestRef.current !== requestId) {
        return;
      }

      setSummary(isObject(response.data?.summary) ? response.data.summary : null);
      setIssuesByType(asArray(response.data?.issuesByType).filter(isObject));
      setIssuesByArtist(asArray(response.data?.issuesByArtist).filter(isObject));
      const nextIssues = asArray(response.data?.issues).filter(isObject);
      setIssues(nextIssues);
      setSelectedIssues((current) => new Set(
        [...current].filter((issueId) =>
          nextIssues.some((issue) => issue.issueId === issueId)),
      ));
      setDashboardPath(path);
      if (!preserveFeedback) {
        setReportMessage('');
        setCopyFallback('');
        setCopyFallbackLabel('');
      }
    } catch (error_) {
      if (!mountedRef.current || dashboardRequestRef.current !== requestId) {
        return;
      }

      setDashboardLoadFailed(true);
      setError(
        error_.response?.data?.message ||
          error_.message ||
          'Failed to load library health data',
      );
    } finally {
      if (mountedRef.current && dashboardRequestRef.current === requestId) {
        setLoading(false);
      }
    }
  };

  const scheduleScanPoll = (delay = SCAN_POLL_INTERVAL_MS) => {
    clearScanTimer();
    if (!activeScanRef.current || document.hidden || scanMonitoringPausedRef.current) {
      return;
    }

    const remaining = pollDeadlineRef.current - Date.now();
    pollTimerRef.current = window.setTimeout(() => {
      pollTimerRef.current = null;
      pollScanStatusRef.current?.();
    }, Math.max(0, Math.min(delay, remaining)));
  };

  const pauseScanMonitoring = (message, type = 'warning') => {
    clearScanTimer();
    scanMonitoringPausedRef.current = true;
    if (mountedRef.current) {
      setScanMonitoringPaused(true);
      setScanMessage(message);
      setScanMessageType(type);
    }
  };

  const pollScanStatus = async (resume = false) => {
    const activeScan = activeScanRef.current;
    if (
      !activeScan ||
      document.hidden ||
      (scanMonitoringPausedRef.current && !resume) ||
      pollRequestRef.current === activeScan
    ) {
      return;
    }

    if (Date.now() >= pollDeadlineRef.current) {
      pauseScanMonitoring(
        'Status checks paused after one minute. The server may still be scanning this library; continue monitoring before starting another scan.',
      );
      loadSummary(activeScan.libraryPath);
      return;
    }

    pollRequestRef.current = activeScan;
    let pauseAfterRequest = false;
    try {
      const statusResp = await libraryHealth.getScanStatus(activeScan.scanId);
      if (!mountedRef.current || activeScanRef.current !== activeScan) {
        return;
      }

      const scanData = isObject(statusResp.data) ? statusResp.data : {};
      const status = scanData.status;
      const filesScanned = Number.isFinite(scanData.filesScanned)
        ? scanData.filesScanned
        : 0;
      const issuesDetected = Number.isFinite(scanData.issuesDetected)
        ? scanData.issuesDetected
        : 0;
      setScanProgress({ filesScanned, issuesDetected });

      if (status === 'Completed' || status === 'Failed' || status === 'Cancelled') {
        resetScanPolling();
        scanMonitoringPausedRef.current = false;
        setScanning(false);
        setScanMonitoringPaused(false);
        if (status === 'Completed') {
          setScanMessage(
            `Scan complete: ${filesScanned} files checked and ${issuesDetected} issues found.`,
          );
          setScanMessageType('positive');
        } else if (status === 'Failed') {
          setScanMessage(
            scanData.errorMessage ||
              `Library Health scan failed after checking ${filesScanned} files.`,
          );
          setScanMessageType('negative');
        } else {
          setScanMessage(
            `Library Health scan was cancelled after checking ${filesScanned} files.`,
          );
          setScanMessageType('warning');
        }
        loadSummary(activeScan.libraryPath);
      } else if (status !== 'Running') {
        setError('The scan status response did not contain a supported status.');
        pauseAfterRequest = true;
        pauseScanMonitoring(
          'The scan may still be running. Retry status checks before starting another scan.',
        );
      } else {
        setScanMessage(
          `Scan in progress: ${filesScanned} files checked and ${issuesDetected} issues found.`,
        );
        setScanMessageType('info');
      }
    } catch (error_) {
      if (!mountedRef.current || activeScanRef.current !== activeScan) {
        return;
      }

      setError(
        error_.response?.data?.message ||
          error_.message ||
          'Failed to poll Library Health scan status',
      );
      pauseAfterRequest = true;
      pauseScanMonitoring(
        'The server may still be scanning. Retry status checks before starting another scan.',
      );
    } finally {
      if (pollRequestRef.current === activeScan) {
        pollRequestRef.current = null;
      }
      if (
        mountedRef.current &&
        activeScanRef.current === activeScan &&
        !scanMonitoringPausedRef.current &&
        !pauseAfterRequest
      ) {
        scheduleScanPoll();
      }
    }
  };

  pollScanStatusRef.current = pollScanStatus;

  const handleStartScan = async () => {
    if (!libraryPath.trim()) {
      setError('Please enter a library path');
      return;
    }

    try {
      resetScanPolling();
      scanMonitoringPausedRef.current = false;
      setScanning(true);
      setScanMonitoringPaused(false);
      setScanProgress({ filesScanned: 0, issuesDetected: 0 });
      setScanMessage('Starting a recursive, read-only scan of the entered server-side path.');
      setScanMessageType('info');
      setError(null);
      const response = await libraryHealth.startScan(libraryPath);
      const scanId = isObject(response.data) && typeof response.data.scanId === 'string'
        ? response.data.scanId
        : '';
      if (!scanId) {
        throw new Error('Library Health scan response did not include a scan id');
      }
      if (!mountedRef.current) {
        return;
      }

      activeScanRef.current = { libraryPath, scanId };
      pollDeadlineRef.current = Date.now() + SCAN_POLL_TIMEOUT_MS;
      setScanMessage('Scan started. Waiting for the first progress update.');
      scheduleScanPoll();
    } catch (error_) {
      resetScanPolling();
      if (mountedRef.current) {
        scanMonitoringPausedRef.current = false;
        setScanMonitoringPaused(false);
        setError(
          error_.response?.data?.message ||
            error_.message ||
            'Failed to start scan',
        );
        setScanning(false);
      }
    }
  };

  const handleResumeScanMonitoring = () => {
    if (!activeScanRef.current) {
      return;
    }

    setError(null);
    scanMonitoringPausedRef.current = false;
    setScanMonitoringPaused(false);
    setScanMessage('Checking the current scan status.');
    setScanMessageType('info');
    pollDeadlineRef.current = Date.now() + SCAN_POLL_TIMEOUT_MS;
    pollScanStatusRef.current?.(true);
  };

  const handleLibraryPathChange = (event) => {
    const nextPath = event.target.value;
    dashboardRequestRef.current += 1;
    setLibraryPath(nextPath);
    setLoading(false);
    setDashboardLoadFailed(false);
    setError(null);
    setReportMessage('');
    setCopyFallback('');
    setCopyFallbackLabel('');

    if (nextPath !== dashboardPath) {
      setDashboardPath('');
      setSummary(null);
      setIssuesByType([]);
      setIssuesByArtist([]);
      setIssues([]);
      setSelectedIssues(new Set());
    }
  };

  const getSeverityColor = (severity) => {
    switch (severity) {
      case 'Critical':
        return 'red';
      case 'High':
        return 'orange';
      case 'Medium':
        return 'yellow';
      case 'Low':
        return 'blue';
      case 'Info':
        return 'grey';
      default:
        return 'grey';
    }
  };

  const getIssueTypeLabel = (type) => {
    switch (type) {
      case 'SuspectedTranscode':
        return 'Suspected Transcode';
      case 'NonCanonicalVariant':
        return 'Non-Canonical Variant';
      case 'TrackNotInTaggedRelease':
        return 'Track Not in Tagged Release';
      case 'MissingTrackInRelease':
        return 'Missing Track in Release';
      case 'CorruptedFile':
        return 'Corrupted File';
      case 'MissingMetadata':
        return 'Missing Metadata';
      case 'MultipleVariants':
        return 'Multiple Variants';
      case 'WrongDuration':
        return 'Wrong Duration';
      default:
        return type;
    }
  };

  const handleToggleIssue = (issueId) => {
    const newSelected = new Set(selectedIssues);
    if (newSelected.has(issueId)) {
      newSelected.delete(issueId);
    } else {
      newSelected.add(issueId);
    }

    setSelectedIssues(newSelected);
  };

  const handleToggleAll = () => {
    const allIssuesSelected =
      issues.length > 0 && issues.every((issue) => selectedIssues.has(issue.issueId));

    if (allIssuesSelected) {
      setSelectedIssues(new Set());
    } else {
      setSelectedIssues(new Set(issues.map((index) => index.issueId)));
    }
  };

  const selectedIssueList = issues.filter((issue) =>
    selectedIssues.has(issue.issueId));
  const selectedFixableIssueIds = getLibraryHealthSafeFixIssueIds(selectedIssueList);
  const selectedAutoFixableCount = selectedIssueList.filter((issue) =>
    issue.canAutoFix).length;

  const handleFixSelected = async () => {
    if (selectedFixableIssueIds.length === 0) {
      setError('Please select issues to fix');
      return;
    }

    const issueIds = selectedFixableIssueIds;
    const remainingAutoFixable = Math.max(0, selectedAutoFixableCount - issueIds.length);

    try {
      setFixing(true);
      setError(null);
      await libraryHealth.createRemediationJob(issueIds);
      setSelectedIssues((current) => new Set(
        [...current].filter((issueId) => !issueIds.includes(issueId)),
      ));
      setReportMessage(
        `Queued remediation for ${issueIds.length} auto-fixable issue${issueIds.length === 1 ? '' : 's'}.${remainingAutoFixable > 0 ? ` ${remainingAutoFixable} remain selected for another batch.` : ''}`,
      );
      await loadSummary(dashboardPath, { preserveFeedback: true });
    } catch (error_) {
      setError(
        error_.response?.data?.message ||
          error_.message ||
          'Failed to create fix job',
      );
    } finally {
      setFixing(false);
    }
  };

  const handleFixSingle = async (issueId) => {
    try {
      setFixing(true);
      setError(null);
      await libraryHealth.createRemediationJob([issueId]);
      setReportMessage('Queued remediation job for 1 auto-fixable issue.');
      await loadSummary(dashboardPath, { preserveFeedback: true });
    } catch (error_) {
      setError(
        error_.response?.data?.message ||
          error_.message ||
          'Failed to create fix job',
      );
    } finally {
      setFixing(false);
    }
  };

  const copyText = async (text, label) => {
    try {
      if (!navigator.clipboard?.writeText) {
        throw new Error('Clipboard access is unavailable');
      }

      await navigator.clipboard.writeText(text);
      setCopyFallback('');
      setCopyFallbackLabel('');
      setReportMessage(`${label} copied to the clipboard.`);
    } catch (_error) {
      setCopyFallback(text);
      setCopyFallbackLabel(label);
      setReportMessage(`Clipboard access failed. Select and copy the ${label.toLowerCase()} below.`);
    }
  };

  const handleCopyReport = async () => {
    const report = buildLibraryHealthReport({
      issues,
      issuesByArtist,
      issuesByType,
      libraryPath: dashboardPath,
      summary,
    });

    await copyText(report, 'Library Health report');
  };

  const handleCopyActionPlan = async () => {
    const plan = buildLibraryHealthActionPlan({
      issues: selectedIssueList,
      libraryPath: dashboardPath,
    });

    await copyText(plan, 'Library Health action plan');
  };

  const handleCopySafeFixManifest = async () => {
    const manifest = buildLibraryHealthSafeFixManifest({
      issues: selectedIssueList,
      libraryPath: dashboardPath,
    });

    await copyText(manifest, 'Library Health safe-fix manifest');
  };

  const handleCopySearchSeeds = async () => {
    const seeds = buildLibraryHealthSearchSeeds({
      issues: selectedIssueList,
      libraryPath: dashboardPath,
    });

    await copyText(seeds, 'Library Health replacement search seeds');
  };

  const handleCopyQuarantinePacket = async () => {
    const packet = buildLibraryHealthQuarantinePacket({
      issues: selectedIssueList,
      libraryPath: dashboardPath,
    });

    await copyText(packet, 'Library Health quarantine review packet');
  };

  const handleRunReplacementSearches = async () => {
    const selectedIssueList = issues.filter((issue) =>
      selectedIssues.has(issue.issueId));
    const queries = getLibraryHealthReplacementSearchQueries(selectedIssueList, {
      limit: 3,
    });

    if (queries.length === 0) {
      setError('Selected issues do not have replacement search candidates.');
      return;
    }

    try {
      setSearchingReplacements(true);
      setError(null);
      const count = await searches.createBatch({ queries });
      setReportMessage(`Started ${count} bounded replacement search${count === 1 ? '' : 'es'} for selected Library Health issues.`);
    } catch (error_) {
      setError(
        error_.response?.data?.message ||
          error_.message ||
          'Failed to start replacement searches',
      );
    } finally {
      setSearchingReplacements(false);
    }
  };

  const renderErrorMessage = () => error ? (
    <Message
      className="library-health-error"
      negative
      role="alert"
    >
      <Message.Content>{error}</Message.Content>
      {dashboardLoadFailed && libraryPath ? (
        <Popup
          className="library-health-popup"
          content="Retry loading saved Library Health results for this path. This does not start another scan."
          trigger={(
            <Button
              aria-label="Retry loading saved Library Health results"
              className="library-health-action"
              disabled={loading || scanning}
              loading={loading}
              onClick={() => loadSummary(libraryPath)}
              type="button"
            >
              Retry Loading Results
            </Button>
          )}
        />
      ) : null}
    </Message>
  ) : null;

  const renderScanMessage = () => scanMessage ? (
    <Message
      className="library-health-scan-message"
      info={scanMessageType === 'info'}
      negative={scanMessageType === 'negative'}
      positive={scanMessageType === 'positive'}
      role="status"
      warning={scanMessageType === 'warning'}
    >
      <Message.Content>{scanMessage}</Message.Content>
      {scanProgress && scanning ? (
        <div className="library-health-progress">
          <strong>Progress:</strong> {scanProgress.filesScanned} files checked;
          {' '}{scanProgress.issuesDetected} issues found.
        </div>
      ) : null}
      {scanMonitoringPaused && activeScanRef.current ? (
        <Popup
          className="library-health-popup"
          content="Resume status checks for the current server-side scan. This does not start a second scan."
          trigger={(
            <Button
              aria-label="Retry Library Health scan status checks"
              className="library-health-action"
              onClick={handleResumeScanMonitoring}
              type="button"
            >
              Retry Status Checks
            </Button>
          )}
        />
      ) : null}
    </Message>
  ) : null;

  const renderActionFeedback = () => reportMessage ? (
    <div aria-live="polite" className="library-health-feedback">
      <Message
        compact
        data-testid="library-health-report-message"
        size="mini"
      >
        {reportMessage}
      </Message>
      {copyFallback ? (
        <label className="library-health-copy-fallback">
          {copyFallbackLabel} — select and copy
          <textarea
            aria-label={`${copyFallbackLabel} text to copy manually`}
            onFocus={(event) => event.target.select()}
            readOnly
            rows={8}
            value={copyFallback}
          />
        </label>
      ) : null}
    </div>
  ) : null;

  const renderOverviewPane = () => (
    <Tab.Pane>
      <Grid stackable>
        <Grid.Row>
          <Grid.Column width={16}>
            <Segment>
              <Header as="h3">
                <Icon name="heartbeat" />
                <Header.Content>
                  Library Health Scanner
                  <Header.Subheader>
                    Detect quality issues, transcodes, and missing tracks.
                    Results are limited to the selected server-side path.
                  </Header.Subheader>
                </Header.Content>
              </Header>
            </Segment>
          </Grid.Column>
        </Grid.Row>

        <Grid.Row>
          <Grid.Column width={16}>
            <Segment className="library-health-scan-controls">
              <label
                className="library-health-path-label"
                htmlFor="library-health-path"
              >
                Library path on the server
              </label>
              <Input
                disabled={scanning}
                fluid
                input={{
                  'aria-label': 'Library path on the server',
                  id: 'library-health-path',
                }}
                onChange={handleLibraryPathChange}
                placeholder="Enter a server path (for example, /music or C:\\Music)"
                value={libraryPath}
              />
              <div className="library-health-actions">
                <Popup
                  className="library-health-popup"
                  content="Load saved health results for the entered server-side path without scanning files. Use this to review a previous scan or recover from a dashboard loading error."
                  trigger={(
                    <Button
                      aria-label="Load saved Library Health results for this path"
                      className="library-health-action"
                      disabled={scanning || loading || !libraryPath.trim()}
                      loading={loading}
                      onClick={() => loadSummary(libraryPath)}
                      type="button"
                    >
                      <Icon name="folder open" />
                      Load Saved Results
                    </Button>
                  )}
                />
                <Popup
                  className="library-health-popup"
                  content="Recursively scan the entered server-side path for audio health issues. This manually starts read-only file inspection with up to four files checked at once; it does not contact peers or modify files."
                  trigger={(
                    <Button
                      aria-label="Start a recursive Library Health scan for this path"
                      className="library-health-action"
                      disabled={scanning || loading || !libraryPath.trim()}
                      loading={scanning && !scanMonitoringPaused}
                      onClick={handleStartScan}
                      primary
                      type="button"
                    >
                      <Icon name="search" />
                      {scanning
                        ? scanMonitoringPaused ? 'Scan Monitor Paused' : 'Scanning...'
                        : 'Start Scan'}
                    </Button>
                  )}
                />
              </div>
              <Message
                className="library-health-scan-guidance"
                info
                size="small"
              >
                Paths are resolved by the server. Scans include subdirectories,
                inspect supported audio files, and leave the files unchanged.
                You can load saved results without starting a scan.
              </Message>
            </Segment>
          </Grid.Column>
        </Grid.Row>

        {renderErrorMessage() && (
          <Grid.Row>
            <Grid.Column width={16}>
              {renderErrorMessage()}
            </Grid.Column>
          </Grid.Row>
        )}

        {renderScanMessage() && (
          <Grid.Row>
            <Grid.Column width={16}>{renderScanMessage()}</Grid.Column>
          </Grid.Row>
        )}

        {loading ? (
          <Grid.Row>
            <Grid.Column width={16}>
              <LoaderSegment>Loading library health data...</LoaderSegment>
            </Grid.Column>
          </Grid.Row>
        ) : summary ? (
          <>
            <Grid.Row>
              <Grid.Column width={16}>
                <Segment>
                  <Statistic.Group widths="three">
                    <Statistic>
                      <Statistic.Value>{summary.totalIssues}</Statistic.Value>
                      <Statistic.Label>Total Issues</Statistic.Label>
                    </Statistic>
                    <Statistic color="red">
                      <Statistic.Value>{summary.issuesOpen}</Statistic.Value>
                      <Statistic.Label>Open</Statistic.Label>
                    </Statistic>
                    <Statistic color="green">
                      <Statistic.Value>
                        {summary.issuesResolved}
                      </Statistic.Value>
                      <Statistic.Label>Resolved</Statistic.Label>
                    </Statistic>
                  </Statistic.Group>
                  <div className="library-health-loaded-path">
                    Results for <strong>{dashboardPath}</strong>
                  </div>
                  <Popup
                    className="library-health-popup"
                    content="Copy a read-only health report for offline review. This does not fix, rescan, quarantine, search, or mutate files."
                    trigger={
                    <Button
                      aria-label="Copy a read-only Library Health report"
                      className="library-health-action"
                      data-testid="library-health-copy-report"
                        disabled={!summary}
                        onClick={handleCopyReport}
                        type="button"
                      >
                        <Icon name="copy" />
                        Copy Report
                      </Button>
                    }
                  />
                  {renderActionFeedback()}
                </Segment>
              </Grid.Column>
            </Grid.Row>

            <Grid.Row>
              <Grid.Column width={8}>
                <Segment>
                  <Header as="h4">Issues by Type</Header>
                  <Table compact>
                    <Table.Header>
                      <Table.Row>
                        <Table.HeaderCell>Type</Table.HeaderCell>
                        <Table.HeaderCell textAlign="right">
                          Count
                        </Table.HeaderCell>
                      </Table.Row>
                    </Table.Header>
                    <Table.Body>
                      {issuesByType.length === 0 ? (
                        <Table.Row>
                          <Table.Cell
                            colSpan={2}
                            textAlign="center"
                          >
                            No issues detected
                          </Table.Cell>
                        </Table.Row>
                      ) : (
                        issuesByType.map((group) => (
                          <Table.Row key={group.type}>
                            <Table.Cell>
                              <Label basic>
                                {getIssueTypeLabel(group.type)}
                              </Label>
                            </Table.Cell>
                            <Table.Cell textAlign="right">
                              <strong>{group.count}</strong>
                            </Table.Cell>
                          </Table.Row>
                        ))
                      )}
                    </Table.Body>
                  </Table>
                </Segment>
              </Grid.Column>

              <Grid.Column width={8}>
                <Segment>
                  <Header as="h4">Top Artists with Issues</Header>
                  <Table compact>
                    <Table.Header>
                      <Table.Row>
                        <Table.HeaderCell>Artist</Table.HeaderCell>
                        <Table.HeaderCell textAlign="right">
                          Issues
                        </Table.HeaderCell>
                      </Table.Row>
                    </Table.Header>
                    <Table.Body>
                      {issuesByArtist.length === 0 ? (
                        <Table.Row>
                          <Table.Cell
                            colSpan={2}
                            textAlign="center"
                          >
                            No artist data available
                          </Table.Cell>
                        </Table.Row>
                      ) : (
                        issuesByArtist.map((group, index) => (
                          <Table.Row key={index}>
                            <Table.Cell>{group.artist}</Table.Cell>
                            <Table.Cell textAlign="right">
                              <strong>{group.count}</strong>
                            </Table.Cell>
                          </Table.Row>
                        ))
                      )}
                    </Table.Body>
                  </Table>
                </Segment>
              </Grid.Column>
            </Grid.Row>
          </>
        ) : (
          <Grid.Row>
            <Grid.Column width={16}>
              <Segment placeholder>
                <Header icon>
                  <Icon name="search" />
                  Enter a library path and start a scan to detect issues
                </Header>
              </Segment>
            </Grid.Column>
          </Grid.Row>
        )}
      </Grid>
    </Tab.Pane>
  );

  const renderIssuesPane = () => (
    <Tab.Pane>
      <Grid stackable>
        <Grid.Row>
          <Grid.Column width={16}>
            {renderErrorMessage()}
            {renderScanMessage()}
            {dashboardPath ? (
              <Message info size="small">
                Showing results for <strong>{dashboardPath}</strong>
              </Message>
            ) : null}

            {selectedIssues.size > 0 && (
              <Segment className="library-health-selection-actions">
                <div className="library-health-selection-count" aria-live="polite">
                  {selectedIssues.size} issue{selectedIssues.size === 1 ? '' : 's'} selected
                </div>
                <Popup
                  className="library-health-popup"
                  content="Queue bounded remediation downloads for up to 25 selected auto-fixable issues. This starts the existing download/remediation workflow; use it when you want slskd to attempt a replacement. The original files are not edited by this control."
                  trigger={(
                    <Button
                      aria-label={`Queue fixes for ${selectedFixableIssueIds.length} selected auto-fixable issues`}
                      className="library-health-action"
                      data-testid="library-health-fix-selected"
                      disabled={fixing || selectedFixableIssueIds.length === 0}
                      loading={fixing}
                      onClick={handleFixSelected}
                      primary
                      type="button"
                    >
                      <Icon name="wrench" />
                      {selectedAutoFixableCount > 25
                        ? `Queue fixes for ${selectedFixableIssueIds.length} of ${selectedAutoFixableCount} auto-fixable issues`
                        : `Queue fixes for ${selectedFixableIssueIds.length} auto-fixable issue${selectedFixableIssueIds.length === 1 ? '' : 's'}`}
                    </Button>
                  )}
                />
                <Popup
                  className="library-health-popup"
                  content="Start bounded live Soulseek replacement searches for selected replacement candidates. This starts searches only; it does not browse peers, download, quarantine, or mutate files."
                  trigger={
                    <Button
                      className="library-health-action"
                      data-testid="library-health-run-replacement-searches"
                      disabled={fixing || searchingReplacements}
                      loading={searchingReplacements}
                      onClick={handleRunReplacementSearches}
                      type="button"
                      aria-label="Start bounded replacement searches for selected Library Health issues"
                    >
                      <Icon name="search" />
                      Start Replacement Searches
                    </Button>
                  }
                />
                <Popup
                  className="library-health-popup"
                  content="Clear selected issues without changing issue status or starting any job. Use this to choose a different review or action set."
                  trigger={(
                    <Button
                      aria-label="Clear selected Library Health issues"
                      basic
                      className="library-health-action"
                      disabled={fixing}
                      onClick={() => setSelectedIssues(new Set())}
                      type="button"
                    >
                      Clear Selection
                    </Button>
                  )}
                />
                <Popup
                  className="library-health-popup"
                  content="Copy a selected-issue action plan for review. This does not create remediation jobs, queue searches, quarantine files, or mutate files."
                  trigger={
                    <Button
                      basic
                      className="library-health-action"
                      data-testid="library-health-copy-action-plan"
                      disabled={fixing}
                      onClick={handleCopyActionPlan}
                      type="button"
                      aria-label="Copy a review plan for selected Library Health issues"
                    >
                      <Icon name="copy" />
                      Copy Action Plan
                    </Button>
                  }
                />
                <Popup
                  className="library-health-popup"
                  content="Copy an auto-fixable issue manifest for review. This does not create a remediation job, execute safe fixes, or mutate files."
                  trigger={
                    <Button
                      basic
                      className="library-health-action"
                      data-testid="library-health-copy-safe-fix-manifest"
                      disabled={fixing}
                      onClick={handleCopySafeFixManifest}
                      type="button"
                      aria-label="Copy a safe-fix manifest for selected Library Health issues"
                    >
                      <Icon name="check circle" />
                      Copy Safe-Fix Manifest
                    </Button>
                  }
                />
                <Popup
                  className="library-health-popup"
                  content="Copy replacement search seed queries for selected issues. This does not open Search, contact peers, download files, or mutate files."
                  trigger={
                    <Button
                      basic
                      className="library-health-action"
                      data-testid="library-health-copy-search-seeds"
                      disabled={fixing}
                      onClick={handleCopySearchSeeds}
                      type="button"
                      aria-label="Copy replacement search seeds for selected Library Health issues"
                    >
                      <Icon name="search" />
                      Copy Search Seeds
                    </Button>
                  }
                />
                <Popup
                  className="library-health-popup"
                  content="Copy a manual quarantine review packet for selected risky issues. This does not change quarantine state, move files, send peer messages, or mutate files."
                  trigger={
                    <Button
                      basic
                      className="library-health-action"
                      data-testid="library-health-copy-quarantine-packet"
                      disabled={fixing}
                      onClick={handleCopyQuarantinePacket}
                      type="button"
                      aria-label="Copy a quarantine review packet for selected Library Health issues"
                    >
                      <Icon name="shield" />
                      Copy Quarantine Packet
                    </Button>
                  }
                />
              </Segment>
            )}
            {renderActionFeedback()}

            {loading ? (
              <LoaderSegment>Loading issues...</LoaderSegment>
            ) : !dashboardPath ? (
              <Segment placeholder>
                <Header icon>
                  <Icon name="search" />
                  Load saved results or scan a library from the Overview tab to see issues.
                </Header>
              </Segment>
            ) : issues.length === 0 ? (
              <Segment placeholder>
                <Header icon>
                  <Icon
                    color="green"
                    name="check circle"
                  />
                  No issues detected
                </Header>
              </Segment>
            ) : (
              <div
                aria-label="Library Health issues table"
                className="library-health-issues-scroll"
                data-testid="library-health-issues-scroll"
                role="region"
                tabIndex={0}
              >
                <Table
                  celled
                  className="library-health-issues-table"
                  selectable
                  unstackable
                >
                <Table.Header>
                  <Table.Row>
                    <Table.HeaderCell collapsing>
                      <label className="library-health-issue-checkbox">
                        <input
                          aria-label={`Select all ${issues.length} loaded Library Health issues`}
                          checked={
                            issues.length > 0 &&
                            issues.every((issue) => selectedIssues.has(issue.issueId))
                          }
                          onChange={handleToggleAll}
                          type="checkbox"
                        />
                      </label>
                    </Table.HeaderCell>
                    <Table.HeaderCell>Type</Table.HeaderCell>
                    <Table.HeaderCell>Severity</Table.HeaderCell>
                    <Table.HeaderCell>Artist</Table.HeaderCell>
                    <Table.HeaderCell>Track</Table.HeaderCell>
                    <Table.HeaderCell>Reason</Table.HeaderCell>
                    <Table.HeaderCell>Status</Table.HeaderCell>
                    <Table.HeaderCell textAlign="center">
                      Actions
                    </Table.HeaderCell>
                  </Table.Row>
                </Table.Header>
                <Table.Body>
                  {issues.map((issue) => (
                    <Table.Row key={issue.issueId}>
                      <Table.Cell collapsing>
                        <label className="library-health-issue-checkbox">
                          <input
                            aria-label={`Select ${[issue.artist, issue.title].filter(Boolean).join(' — ') || issue.issueId}`}
                            checked={selectedIssues.has(issue.issueId)}
                            onChange={() => handleToggleIssue(issue.issueId)}
                            type="checkbox"
                          />
                        </label>
                      </Table.Cell>
                      <Table.Cell>
                        <Label
                          basic
                          size="small"
                        >
                          {getIssueTypeLabel(issue.type)}
                        </Label>
                      </Table.Cell>
                      <Table.Cell>
                        <Label
                          color={getSeverityColor(issue.severity)}
                          size="small"
                        >
                          {issue.severity}
                        </Label>
                      </Table.Cell>
                      <Table.Cell>{issue.artist || '-'}</Table.Cell>
                      <Table.Cell>{issue.title || '-'}</Table.Cell>
                      <Table.Cell>
                        <span title={issue.reason}>
                          {issue.reason?.length > 50
                            ? issue.reason.slice(0, 50) + '...'
                            : issue.reason}
                        </span>
                      </Table.Cell>
                      <Table.Cell>
                        <Label
                          color={
                            issue.status === 'Resolved'
                              ? 'green'
                              : issue.status === 'Fixing'
                                ? 'blue'
                                : issue.status === 'Failed'
                                  ? 'red'
                                  : 'grey'
                          }
                          size="mini"
                        >
                          {issue.status}
                        </Label>
                      </Table.Cell>
                      <Table.Cell textAlign="center">
                        {issue.canAutoFix && issue.status === 'Detected' && (
                          <Popup
                            className="library-health-popup"
                            content="Queue a remediation job for this auto-fixable issue. The backend still applies its remediation safeguards."
                            trigger={
                              <Button
                                aria-label={`Queue a remediation job for ${[issue.artist, issue.title].filter(Boolean).join(' — ') || 'this issue'}`}
                                className="library-health-action"
                                disabled={fixing}
                                onClick={() => handleFixSingle(issue.issueId)}
                                primary
                                size="tiny"
                                type="button"
                              >
                                <Icon name="wrench" />
                                Fix
                              </Button>
                            }
                          />
                        )}
                        {issue.status === 'Fixing' && (
                          <Loader
                            active
                            inline
                            size="tiny"
                          />
                        )}
                      </Table.Cell>
                    </Table.Row>
                  ))}
                </Table.Body>
                </Table>
              </div>
            )}
          </Grid.Column>
        </Grid.Row>
      </Grid>
    </Tab.Pane>
  );

  const panes = [
    {
      menuItem: {
        content: 'Overview',
        icon: 'dashboard',
        key: 'overview',
      },
      render: renderOverviewPane,
    },
    {
      menuItem: {
        content: 'All Issues',
        icon: 'warning',
        key: 'issues',
      },
      render: renderIssuesPane,
    },
  ];

  return (
    <div
      aria-busy={scanning || loading}
      className="library-health"
    >
      <Tab
        activeIndex={activeIndex}
        onTabChange={(_event, { activeIndex: nextIndex }) =>
          setActiveIndex(nextIndex)
        }
        panes={panes}
      />
    </div>
  );
};

export default LibraryHealth;
