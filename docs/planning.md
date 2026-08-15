# tech
1. Database server - sql server container
13. multi layer 
    - .net core 8 + grpc
    - .net 4.8 + wcf
4. github actions + repo + pages
<!-- 6. audit ?
    - users
    - accounts   -->
7. tests
    - unit tests
    - integration tests with testcontainers
9. swagger
3. Traceability - jaeger + seq
10. redis cache ?
8. wcf ?
<!-- 11. login ? - custom or cloak key -->
14. setup + seeding scripts
15. docker compose file + docker image
16. architecture
    - ***clean architecture***
    - ***layers***
        - ui
            - components
                - dashboard
                - account setup
                - create movement
                - get balance
                - statement
            - services
                - get
                - movement
                - balance
                - statement
        - presentation
            - grpc
                - **error handling** 
            - application
                - mediatr
                - logging pipeline
                - validation pipeline
        -  domain
            - private services + public interfaces
            - di configs in each layer.
            - domain models 
                - sealed classes
                - public getters + private setters
                - constructor with guard clauses - ensure least permission
                - validation - fluent validations
        - infrastructure
            - ef core ? 
11. standards - 
    - for each service -> create and implement an interface
    - service must be private if implementing an inteface + be sealed
    - use primary constructor to inject services into other services
    - use readonly properties - domain objects
        - set property values through constructors
        - validate all values according to type in constructor before assigning values to properties
        - throw exception if property value are invalid
    - instead of throwing exceptions in methods:
        - return Result type - result design pattern
    - use global exception handler - return problemdetails
    - create static extension method in each layer to register services with di container
        - call each register extension method in web api program.cs
    - NB - implement each layer using TDD
12. documentation
    - diagrams
     - sequence
     - activity
     - data

# business rules
1. pk - composite primary key - 
    - accountId - ACC-001 - varchar - max 10
    - externalRef - MOV-20240715-000123 - varchar - max 20
4. exception handling - result pattern
5. ### Handle large statement exports in a way that does not require the client or server to hold the entire result in memory at once 
    - grpc specific - maybe caching ???
6. ### Operations staff record cash movements (deposits, withdrawals, transfers), view balances per account, and export large statements.
    - .net 8 web api
        - deposits
            - post (dto)
            - idempotency
        - transhers
            - post (dto)
            - idempotency
        - view balances
            - get (accountId)
        - export statements 
            - get (accountId + dateTime)

# data
1. ### new consumers (accounts)
    - accountId 
        - ACC-001 - varchar - max 10
    - account_name - varchar - max 20
    - contact_person - varchar - max 30
    - createdDate - datetime
    - createdBy - user id
2. ### movement
    - between accounts -> 1 customer multiple accounts
    - movement operation fields
        1. externalRef: example MOV-20240715-000123
        2. accountId: example ACC-001
        3. currency: example ZAR
        4. amount: example 12500.00 (positive for deposit, negative for withdrawal)
        5. occurredAt: example 2024-07-15T10:42:31Z
        6. narration: example Initial deposit
        7. ref nr: guid
        8. movedBy: user Id
        9. movedDate: datetime
3. ### users
    - id
    - name + surname    

