---
category: security
audience: operators
area: logging
action: none
breaking: false
---
Escape peer usernames, remote filenames, local paths, policy text, and exception details in transfer enqueue, retry, upload, replacement, verification, and controller diagnostics. Sanitization is applied only to log fields, so transfer inputs and returned values retain their original contents.
