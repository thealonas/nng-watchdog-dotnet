# nng watchdog

[![License badge](https://img.shields.io/badge/license-EUPL-blue.svg)](LICENSE)
[![GitHub issues](https://img.shields.io/github/issues/MrAlonas/nng-watchdog)](https://github.com/MrAlonas/nng-watchdog/issues)
[![Docker Build and Push](https://github.com/MrAlonas/nng-watchdog/actions/workflows/docker.yml/badge.svg)](https://github.com/MrAlonas/nng-watchdog/actions/workflows/docker.yml)

Скрипт для групп nng, отслеживающий нарушения правил редакторами. В случае нарушения правил пользователь автоматически блокируется, а совершенные нарушения отменяются.

## Установка

Воспользуйтесь готовым [Docker-контейнером](https://github.com/orgs/MrAlonas/packages/container/package/nng-watchdog).

По умолчанию используется порт `1221`, поэтому необходимо использовать прокси-сервер (например nginx).

## Настройка

### Переменные среды

* `REDIS_URL` — Ссылка на базу данных
