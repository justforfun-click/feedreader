FROM justforfunclick/ubuntu:latest

ARG AdminKey
ARG ApiBase

COPY . /app

RUN apt update && apt install -y cron

RUN mkdir -p /var/log/feedreader && cd /app && npm install

RUN echo "* * * * * root node /app/refresh_feeds.js --admin-key $AdminKey --api-base $ApiBase" >> /etc/crontab

CMD ["cron", "-f"]
