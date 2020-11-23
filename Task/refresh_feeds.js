const request = require("request-promise");

var args = process.argv.slice(2);
if (!args) {
    console.log("Usage node refresh_feeds.js --admin-key <xx> --api-base <xxxx>");
    return;
}

var options = {
    adminKey: null,
    apiBase: null
};

for (var i = 0; i < args.length; ++i) {
    if (args[i] == "--admin-key") {
        options.adminKey = args[++i];
    } else if (args[i] == "--api-base") {
        options.apiBase = args[++i];
    }
}
if (!options.adminKey || !options.apiBase) {
    console.log("Usage node refresh_feeds.js --admin-key <xx> --api-base <xxxx>");
    return;
}

for (var i = options.apiBase.length - 1; i >= 0; --i) {
    if (options.apiBase[i] != '/') {
        options.apiBase = options.apiBase.substr(0, i + 1);
        break;
    }
}

(async ()=> {
var data = await request(`${options.apiBase}/@admin/get-feed-uri-list`, {
    headers: {
        "AdminKey": options.adminKey
    }
});

var feedURIs = JSON.parse(data);
for (var i in feedURIs) {
    var feedUri = feedURIs[i];
    console.log(`Refresh: ${feedUri}`);
    try {
        await request(`${options.apiBase}/@admin/update-feed`, {
            headers: {
                "AdminKey": options.adminKey
            },
            qs: {
                "feed-uri": feedUri
            }
        });
    } catch (e) {
        console.log(`failed: ${e}`);
    }
}
})();
