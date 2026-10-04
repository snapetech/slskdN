---
category: changed
audience: users, operators
area: packaging
action: none
breaking: false
---
Runtime packages no longer include unused Roslyn compiler assemblies, reducing the application payload by about 10 MB. The optional source-analysis task stays in the dedicated tooling project.
