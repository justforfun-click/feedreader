var https = require("https");
var url = require("url");
var request = require("request");
var fs = require("fs");

https.createServer({
    key: fs.readFileSync("/etc/letsencrypt/live/proxy.feedreader.org/privkey.pem"),
    cert: fs.readFileSync("/etc/letsencrypt/live/proxy.feedreader.org/fullchain.pem")
}, onRequest).listen(443);

function onRequest(req, res) {
    var queryData = url.parse(req.url, true).query;
    if (queryData.url) {
        request({
            url: queryData.url
        }).on('error', function(e) {
            res.end(e);
        }).pipe(res);
    } else {
        res.writeHead(404);
        res.end();
    }
}
