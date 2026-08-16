1. Read planning.md and first_draft.md as context
2. Inside src folder - create 2 folders:
    - project 1:
        - name = sygnia.frontend
            - angular 18

    - project 2:
        - name = sygnia.backend 
            - .net 8 Solution
            - separate projects using clean architecture.
                - sygnia.presentation
                - sygnia.application
                - sygnia.domain
                - sygnia.infrastructure
3. Compile and build projects
4. For .net solution - implement Directory.Build and Directory.Packages for centralization
5. Add node_modules to .gitignore
6. Create 2 solution folders in Sygnia.Backend
    - 1. src
        - move all the projects except the tests folder into src
    - 2. tests
        - move Sygnia.UnitTests into the tests solution folder. Delete the original tests folder/
        - Rename Sygnia.UnitTests to Sygnia.Tests
            - reason - Sygnia.Tests will be for unittests and integration tests
7. Add and configure:
    - Seq and Serilog - using docker image
    - Jaeger to test tracing
8. Add editor.config file with theses entries:
    look at "X:\Tutorials\DomeTrain\01_ModularMonoliths\Intro\src\.editorconfig" for content.
9. Create github actions:
    - as a ci/cd pipeline - only include build stage for both client and backend
    - separate the backend and client build into 2 files.
10. Ensure all services, database access and grpc access contains async/await asyncronousity 
11. Setup a loop with max loop couter of 3. for each iteration:
    - investigate the code quality of backend and client
    - make suggestions
    - wait untill suggestion is approved
    - create new branch, implement fix and wait for approval
    - once approved, commit change and merge with main

** Additions
12. Plan to include a wcf in .net 4.8. It should get the same values like balances as using the grpc endpoints.
13. Plan to create a small wpf application to query the balance. Does not have to be an complext app.
14. The wpf application view must have a dropdwon list with accounts to select from. The user might know the account id.

** Modifications
1. Whole application - improve on comments by replacing long comments with shorter, concise, relevant comments.
2. Investigate xcode complexity. Logic should not be over-engineered and simple.
3. Cleanup all unneeded files such as unused png, md etc.

4. Review code using supoerpowers skill
5. Ensure classes only do one function
6. Give class and method names meaningful values
7. Methods should not contain more than 15 lines of code
8. Create a docker compose file to create a docker image of all the projects + database. Should be uploadable to dockerhub.
9. Also create a script that will:
    - run the schema and seed script if not run already.
    - start the Sygnia.Presentation project
    - start the Sygnia.WcfGateway
    - stop the above projects
10. Create an docx manual to explain the high level design. Must include wellformatted diagrams created by draw.io - use draw.io mcp server.
    - Include activity diagram to explain backend.
        - Include swimlanes
        - use color in process shapes
    - Include sequence diagram
    - Store the docx in docs folder.
        - The document should explain the techstack and implementation to a junior developer and non-technical stakeholder.
        - Create professional document.
11. Add github pages to advertise the application.
12. Allow user to download the manual created in point 10 from the github page 