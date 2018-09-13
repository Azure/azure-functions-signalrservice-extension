// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

module.exports = function (context, req, connectionInfo) {
  // Azure function doesn't support CORS well, workaround it by explicitly return CORS headers
  context.res = {
    body: connectionInfo,
    headers: {
      'Access-Control-Allow-Credentials': 'true',
      'Access-Control-Allow-Origin': req.headers.origin,
      'Access-Control-Allow-Headers': req.headers['access-control-request-headers']
    }
  };

  context.done();
};