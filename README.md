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

* `LogUser` — Айди страницы человека, которому будут отправляться логи
* `DialogGroupToken` — Токен группы, от которой отправляются логи
* `UserToken` — Токен страницы, от которой выполняются действия
* `BanComment` — Комментарий при блокировке пользователя

### groups.json

```json
{
  "Айди группы": {
    "Confirm": "Строка, которую должен вернуть сервер",
    "Secret": "Секретный ключ"
  }
}
```
