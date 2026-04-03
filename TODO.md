* Refactor to [Clean](https://devblogs.microsoft.com/ise/next-level-clean-architecture-boilerplate/) project setup
* Move DB out of the artifact, to something external like PGSQL
* Track the number of iterations, and initial board state, for display/auditing
    * Also allows us to do "set the board state to iteration X" instead of "advance X iterations"
* Use custom exceptions and middleware for changing HTTP response code, instead of keeping it in controller methods
* Switch validation to FluentValidation or similar library
* Log to external log store (splunk, elk, etc)