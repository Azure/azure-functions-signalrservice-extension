// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

module.exports = function (context, req, endpoints) {
  context.bindings.signalRMessages = [{
    "userId": req.body.recipient,
    "groupName": req.body.groupname,
    "target": "newMessage",
    "arguments": [req.body],
    "endpoints": endpoints.filter(endpoint => endpoint.name !== "") // if this field is not set, send to all endpoints
  }];
  context.done();
};