module.exports = function (context, req, token) {
  context.res = { body: token };
  context.done();
};