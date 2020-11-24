const lockfile = require('proper-lockfile');
const request = require("request-promise");
const log4js = require("log4js");

const options = {
    adminKey: "local.test.admin.key",
    apiBase: "http://localhost:7071/api",
    logLevel: "info",
};

// Run as single instance.
var log = null;
lockfile
.lock(`${__dirname}/refresh_feeds.lock`, {realpath: false})
.then(() => main())
.catch(e => { (log || console).log(e); process.exitCode = 1; });

async function main() {
    // Config log
    log4js.configure({
        appenders: {
            console: {
                type: "console"
            },
            file: {
                type: "file",
                filename: `${__dirname}/log/refresh_feeds.log`,
                maxLogSize: 1 * 1024 * 1024,
            }
        },
        categories: {
            default: {
                appenders: ['console', 'file'],
                level: options.logLevel
            }
        }
    });
    log = log4js.getLogger("feedreader.refres_feeds");

    // Parse options.
    var args = process.argv.slice(2);
    for (var i = 0; i < args.length; ++i) {
        switch (args[i]) {
            case "-h": console.log("Usage: node refresh_feeds.js --admin-key <xx> --api-base <xxxx>"); process.exit(0);
            case "--admin-key": options.adminKey = args[++i]; break;
            case "--api-base": options.apiBase = args[++i]; break;
            case "--log": options.logLevel = args[++i]; break;
            default: throw `Unknown options '${args[i]}'`;
        }
    }

    // Reset log level.
    log.level = options.logLevel;
    
    // Normalize api base.
    for (var i = options.apiBase.length - 1; i >= 0; --i) {
        if (options.apiBase[i] != '/') {
            options.apiBase = options.apiBase.substr(0, i + 1);
            break;
        }
    }

    // Log api base.
    log.info(`Refresh starts, apiBase: ${options.apiBase}`);
    
    // Get feed uri list.
    var data = await request(`${options.apiBase}/@admin/get-feed-uri-list`, {
        headers: {
            "AdminKey": options.adminKey
        }
    });

    // Refresh feed one by one.
    var feedURIs = JSON.parse(data);
    var failedCount = 0;
    log.info(`${feedURIs.length} feeds need be refreshed.`);
    for (var i in feedURIs) {
        var feedUri = feedURIs[i];
        try {
            log.debug(`Refresh feed: ${feedUri}`);
            await request(`${options.apiBase}/@admin/update-feed`, {
                headers: {
                    "AdminKey": options.adminKey
                },
                qs: {
                    "feed-uri": feedUri
                }
            });
        } catch (e) {
            ++failedCount;
            log.error(`Refresh ${feedUri} failed: ${e}`);
        }
    }
    log.info(`Refresh finished, ${failedCount} feeds are failed to refresh.`);
}
