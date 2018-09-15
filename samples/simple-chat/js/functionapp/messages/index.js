// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

module.exports = function (context, req) {
  // Azure function doesn't support CORS well, workaround it by explicitly return CORS headers
  context.res = {
    headers: {
      'Access-Control-Allow-Credentials': 'true',
      'Access-Control-Allow-Origin': req.headers.origin,
      'Access-Control-Allow-Headers': req.headers['access-control-request-headers']
    }
  };

  if (req.method === 'POST') {
    const message = req.body;
    const recipient = req.query.recipient;

    const signalRMessage = {
      "target": "newMessage",
      "arguments": [ message ]
    };

    if (recipient) {
      message.text = "(private message) " + message.text;
      signalRMessage.userId = recipient;
    }

    context.bindings.signalRMessages = [ signalRMessage ];
  }

  context.done();
};