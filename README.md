# DBManagement
The quick & dirty ORM for C#.
* Currently supporting **MySQL** and **SQLite**.

## Explanation
I developed this alongside another project, typical _create/load an object in the database, modify its fields and save it again_ project. Thanks to this ORM I cut development time exponentially, so I thought that maybe it could be useful for other people, mainly for small/home projects, PoCs where you dont want anything fancy.

**Forget there is a database, just use it!**

## Quick tutorial
- Add a reference directly to the project, or build the DLL and add a reference to it.
- Make the classes you want to be _DBManaged_ childs of DBEntity<T> class.
- It will manage public properties
- Call CreateTable() to automatically generate a table in the DB based on the class properties.
- Now you can use New(), Load(), Modify() and Delete() to do your stuff against the database.
- That's it!

### Interesting points
- You can create the whole database by just calling T.CreateTable() on each object that needs DB supports.
- The library supports IList<T> properties where T is another DBEntity child. For this you need to CreateTable() for both classes AND CreateTableRelation<T1, T2>(). Now the library will be smart enough to create, modify and delete the objects inside the list in the database when you Modify() the object containing the list.

### Limitations
- Object IDs are hardcoded to ints in the database. This is to simplify all the design, you can't touch the ID anyway.
- Probably a lot more. Remember, quick&dirty!