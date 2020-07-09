FeedReader
==

[![Build Status](https://dev.azure.com/xieyubo/build/_apis/build/status/feedreaderorg.feedreader?branchName=master)](https://dev.azure.com/xieyubo/build/_build/latest?definitionId=11&branchName=master)

This is a learning project. The target goal is building an online web feed reader with blazor web assembly
and azure functions.

## Goal

- Client is built with blazor web assembly. It's a single page application (SPA) which can be loaded by the
  browser and exected inside the browser.
- Most of the logics are in client side. Server is very thin. It just saves the users' feeds so that users
  can read their feeds on another machine.
- Most server functions should be implemented by the azure functions. Server cost should be as lower as it
  can. If possible, avoid any "pay by hours" service, only select "pay as you go" service.
- Support third-pary account login. E.g, Microsoft account, Google account, Facebook account. Don't handle
  user registration. It's annoying to user to register a new account again and again.
- Support RSS, ATOM, Json Feed standard.
- Free! Free! Free!
- Maybe more ... 

Currently, I only have the goal, but don't know how to implement it. That's why I create this project and
try to learn by doing.
