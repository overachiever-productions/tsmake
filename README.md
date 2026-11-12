# tsmake

Build Framework for T-SQL Libraries. 

## Pronunciation
- "smake" - like "snake" - but with an 'm'.
- "tee-ess-make" - ts-make. 
- "t-sql make" - cuz... that's what it is. 

## Scope
With larger T-SQL based projects or frameworks, there's a balancing act that needs to be addressed in terms of managing source-code (version, isolation, etc.) vs bundling/simplicity-of-deployment. 

For example, S4: 
- has roughly 60K lines of source code. 
- deploys as a single .sql file (for both "installs" and "updates")

Stated differently: 
- To simplify deployment, I could keep all 60K lines of source-code in ... a single.tsql file. 
    - this'd make deployment easier
    - at the expense of bogging my IDE, adding insane cognitive load for changes, and making versioning of changes a veritable joke. 
- But, I also don't want to deploy S4 with a 'runner' or .exe/.bat/.ps1 file. Those add security implications that no one should be comfortable with in today's security environment. 
    
To balance both of these needs/concerns: 
- I edit, develop, and 'store' my 60K lines of code in 300+ different files (some for tables, some for UDTs, some for funcs/sprocs, and so on). 
    - This is still a chore - but it's NOT the nightmare that dealing with all of this code would be if it were in, lol, a single .sql file. 
- When I'm ready for deployment, I run a build-script/'engine' on my end that uses an xxx.build.sql file (with directives for the inclusion (ordering), formatting, and dynamic-string replacement (i.e., tokens for things like 'version' or 'summary' details)) to generate a single .sql file when all is said and done. 

### Longer Term Goals
#### Migrations
In addition to simplifying this 'build' process for my own projects, a 'build tool' for T-SQL scripts has applicability outside of the very narrow realm of "weirdos who create entire frameworks in T-SQL" - in the sense that DEVELOPERS building applications against SQL Server databases are constantly looking for ways/options to BUNDLE their 'migrations' from one 'branch'/release to the next. Long-term, one of the goals with tsmake is to let it build 'runners'/migration-scripts via a hand-ful of directives when pointed against a specific Git branch or similar. 

#### Inline T-SQL Documentation
As a framework builder/developer, I get SUPER sick of trying to juggle documentation in EXTERNAL files/resources - instead of being able to 'drop it into source code' where it belongs. 
e.g., C# and PowerShell have INLINE documentation. 
tsmake has INITIAL pipelines, syntax-rules, and workflows (still very much proof-of-concept) to allow similar 'conventions' for being able to 'decorate' T-SQL with options for header-level "inline" documentation. Better yet, as a build-engine, tsmake allows this documentation to be stripped/removed during BUILDS of .sql files (as an option - not required) AND provides options for "documentation builds/passes" - where you can 'build' documentation - i.e., use your SAME xxx.build.sql file to iterate over all of your .sql files and ... extract "inline" documentation from each file ... and then export/output via simple 'writers' that can spit out .txt, .md, and the likes. 

## Guesstimated Roadmap
All of the above is ... ambitious. Meaning that features/functionality will have to be released in stages. 
I've also formulated, reformulated, and reformulated (a few times - over the years) how "all of the above" will work together while trying to be simple to use and as obvious to discover as possible. I've also tried multiple implementations - line-by-line iterations, syntax-trees, tokenizers, and the likes. Some grew way too complicated. Others were fine but suffered from performance challenges and some cognative dissonance in terms of implementation. 

Non-released v0.3 is a proof of concept. Stubbs for most features/functionality identified above are roughly in place. In short, scaffolding is in place. And the current version / implementation is extensible and FAST (i.e., there's no excuse/reason in my mind for why a build tool like tsmake should take more than 1 (possibly 2 - tops) seconds to iterate over hundreds of TEXT (.sql) files and generate either docs (via extraction) and/or spit out a single .sql file as a build artifact). 

- 0.3 - Scaffolding / Proof of Concept - with logic for recursive ##FILE and ##DIRECTORY includes working - along with initial workflows for BUILD condition evaluation (#Flags) vs runtime conditional-logic in place. 
- 0.4 - MVP integration of TOKENS / Token Replacement - including non-matched token behavior options. 
- 0.5 - Finalize BUILD and RUNTIME conditionals (FLAGS() and CONDITION() syntax). 
- 0.6 - BUILD Transforms. Optional transformations "post-build" - (e.g., target-shaping and other features to change, say: `[dbo].[xxxx]` to a different schema - even if/when code might contain `dbo`, or `[dbo]` and similar variants/etc.)
- 0.7 - CONFIG file integration - Simplify command-line inputs by means of myproject.build.json as a container for your build preferences/etc. (per project or user-wide/etc.)
- 0.8 - INLINE Document Extraction + syntax-rules finalized + initial .txt and .md transforms/outputs for documentation artifacts. 
- etc... 


