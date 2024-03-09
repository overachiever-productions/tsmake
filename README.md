# tsmake

### Roadmap
What follows is a VERY high-level guestimate of what the roadmap for tsmake will look like: 
- 0.3 Core ‘build’ framework - i.e., a replacement for S4buildPrototype - but fully ‘dynamic’ or ‘syntax-ified’ useable for admindb, dda, badger, and anything else - without having to copy/paste/tweak a VS project…
- 0.4 Improved error-handling, streamlined workflow/pipeline, and core helper funcs complete.
- 0.5 will include INLINE syntax parsing and core/basic output (markdown).
- 0.6 will add Formatter implementations.
- 0.7 will add a plain-text formatter. and then I’ll extend it for dda, admindb, and batcher to support a ‘help system’ for each of those projects.
- 0.8 will stub in the idea of a migration-runner and make sure all core ideas/functionality needed to make that kind of ‘stuff’ work will be accounted for in basic BUILD files/functionality to date.
- 1.0 will be docs and everything else.
- 1.3 will include the runner - implemented via PSI.
- 1.6 will be bug-fixes and initial interfaces for a migration-builder - or the option to ‘query’ some sort of file-system/file-manager and get a list of modified files that can be included via various directives.
    - 1.7 will implement a basic file-system ‘builder’ - i.e., the ‘system’ to query is the OS’ file-system with a ‘modified-since’ timestamp.
    - 1.8 might be the same as above - but for … dropbox? (any files in a given folder - modified since x).
    - 1.9 would be an ONLINE git-fetch implementation.
- 2.0 would be posh-git - for local …
- 2.1 would be bug-fixes and the likes.
    - from here, people can extend tsmake as needed to account for:
        - Formatters/Transformers
        - Build-Query-able-Lists - for Build-Creation.