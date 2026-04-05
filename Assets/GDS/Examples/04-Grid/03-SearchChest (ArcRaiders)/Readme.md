### Search chest (Arc Raiders)

This example demonstrates a way to implement the mechanic of **searching a chest** (or bag), as seen in games like **Arc Raiders** and **Escape from Tarkov**.

When a player opens a chest, its contents are searched—items are identified one by one.

Until all items are identified, nothing can be added to or removed from the chest. If the chest is closed, the process is stopped and then resumes with the next unidentified item when it is reopened.

The mechanic is implemented in `Arc_Chest.cs` using a sequence of asynchronous tasks.