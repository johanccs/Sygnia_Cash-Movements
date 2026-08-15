** Context
1. The Sygnia.Infrastructure project is used to:
    - persist move, user and account to the database
    - query statements and balance

2. Use ef core as orm for .net 8
3. A service can be injected into the application commandhandlers and queryhandlers or the dbContext itself can be used.
4. When using ef core do the following
    - Use AsNoTracking when querying data and the state is not going to be changed. - help to improve performance.
    - Use projections (.select(x=>x.Id,x.Name)) to select only the columns that are needed for the query. DO NOT just select all columns (Select * from)
    - If a query is executed against tables that are joined using a pk/fk relationship, use the include(x=>x.Table) to remove the n+1 antipattern.
5. Create an Entities folder with the same classes to accomplish the use cases in Sygnia.Application. Implement navigation properties if needed.
6. Create migration scripts that can be executed when on a new computer.
7. Create migrations for each class.
8. Use sql server on localhost (docker)
    - username: sa
    - pasword: @1Mops4moa
    - initial catalog: sygnia_cash
9. Ensure that when movement operation is performed, full acid compliance
10. Use superpowers to do PR reviews
11. Write unit tests and integration tests
12. Use lazy loading in linq statements as much as possible.
13. Infrastructure / Application must convert between domain models and efcore entities