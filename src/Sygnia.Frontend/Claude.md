** Context
1. Backend solution in this root is a net web api
2. The current frontend project is an angular 20
3. Communication to the backend should be implementing grpc

** Instructions
** In scope for this branch

1. Create an angular application.
2. Create a basic homepage component
3. Use bootstrap 5 for css library

4. Create a basic accounts component to add an account
5. Movement and Balance are their own top-level menu items/components (`/movement`, `/balance`),
   not tabs under User. The User component/menu instead creates the "normal user" record that a
   movement or transfer's `MovedBy` field is attributed to, for audit purposes — id, name,
   surname only for now. It does not perform movements, balance checks, or statements itself.
6. Create a statement component - should filter according to account and date
    - must be able to handle > 50 000 items -> should be performant. 
    - include pagination
        - change backend code to support pagination
        - add statement preview component.
        - let user download statement as pdf
7. Create grpc services to query backend
8. Use sygnia logo from https://www.sygnia.co.za/ as brand icon and favicon
9. Add menu at the top to navigate between components
10. Give a name as tab header in browser - ![alt text](image.png)

** Modifications
1. Text / Input controls ![alt text](image-1.png) - use small class
2. ![alt text](image-2.png) - for accountId field - supply a dropdown control with all account name to select from.
3. ![alt text](image-3.png) - for currency - use drop down to select from major currencies to select
4.  ![alt text](image-4.png) - reduce top margin
5.![alt text](image-5.png) - hide Transfer tab
