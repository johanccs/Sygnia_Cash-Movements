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