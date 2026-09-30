# ADR-0026: React 19 with Fomantic UI React

**Status:** Accepted  
**Date:** 2026-09-30

## Context

The Web UI was on React 18.3.1 because Semantic UI React 2.1.5 pulls in a ref
helper that calls the removed `ReactDOM.findDOMNode` API. The published
`react-fomantic-ui@3.0.0-beta.5` package also lacks the React 19 fixes present
in upstream PR #49, and its published peer ranges stop at React 18.

The PR #49 head builds and passes its library suite on React 19.3.0 and React
18.3.1. Its source and CommonJS/ESM outputs are available at commit
`f48309b333505a3df02499d7c97cd2ebaef3d385`, but the upstream PR is still open.
This application also depended on v2 component-instance refs and the `<Ref>`
wrapper in Browse, Users, Search, Chat, and Rooms.

## Decision

Use React and React DOM 19.3.0 with `react-fomantic-ui@3.0.0-beta.5` aliased to
the existing `semantic-ui-react` import name. Apply the checked-in pnpm patch
containing the upstream PR #49 source and CommonJS/ESM outputs. Use pnpm
overrides for the React peer ranges of Fomantic UI React, its event stack, and
`react-popper`, and remove the unused Fluent event-listener dependency.

Update application refs to use the fork's direct DOM refs, and replace the two
`<Ref innerRef>` wrappers with native refs. Keep the React 19 patch until an
upstream published release contains the same fixes and passes the Web suite.
The local package patch also returns Fomantic Portal's timer cleanup from its
effect, canceling delayed hover callbacks when a portal unmounts.

## Consequences

- The full Web suite validates the app against React 19.3.0 and the patched
  component library; production build, Web lint, and repository lint also pass.
- `pnpm peers check` no longer reports React or React DOM incompatibilities;
  its remaining ESLint plugin peer-range warning predates this migration.
- The component-library patch must stay synchronized with its pinned upstream
  commit and be removed only after a published replacement is validated.
- Fomantic UI React 3 changes component ref shapes, so new app code and future
  upgrades must use direct DOM refs.
- Portal hover timers are canceled on unmount; a regression test covers teardown
  while an open delay is pending.
