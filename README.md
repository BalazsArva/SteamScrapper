# SteamScrapper

A hobby project that I develop to play around with Redis and use it to coordinate distributed work.

# Setup

Required:

* MS SQL
* RavenDB
* Redis
* Dotnet + Visual Studio

# Applications

## SteamScrapper.Crawler

This app's job is to crawl the Steam store. It starts off from one or more starting URLs, downloads the HTML content for that URL, extracts all Steam store links, compares them against an allowance list (e.g. don't scan legal notices), and registers the allowed links for exploration. While at it, it also registers any URLs pointing to apps/bundles/subs into an SQL Server database, marking them as non-explored.

It maintains the to-be-explored and explored sets in Redis, using hashsets. These hashsets have a date stamp in their keys, making sure that the next day the process will start from scratch.

## SteamScrapper.AppScanner

This app's job is to scan those apps that are present in the database. The app records in the database are inserted by the crawler application as they are encountered, and when they are first inserted into the database, they are marked as "Unknown". The app scanner will take a bunch of app records from the database that have not yet been scanned on the date of execution (when the records are first created, they have a fake, long-past date as last scan date, such as 2000-01-01 to ensure that the app scanner can pick the up immediately). As it processes those records, it downloads the HTML content for them and extracts some bits of information, such as title, banner image, etc.

To ensure that multiple instances of the app scanner can run concurrently without duplicated work, they insert reservations into Redis as they get the to-be-scanned items from the database. These reservations are just keys without any useful value, but with TTL set on them. The presence or absence of such keys specify whether the app with a given Id is scanned by another instance. When the scanning is complete, the DB record is updated, marking is as "scanned today", so other instances won't consider it for processing. If the process stops before fully processing a record though, then the corresponding Redis key's TTL will eventually expire. This means that if the DB record is not yet updated, then any subsequent queries will return that item and the expiration of the Redis reservation also enables reprocessing.

## SteamScrapper.BundleScanner

This app's job is to scan those bundles that are present in the database. The bundle records in the database are inserted by the crawler application as they are encountered, and when they are first inserted into the database, they are marked as "Unknown". The bundle scanner will take a bunch of bundle records from the database that have not yet been scanned on the date of execution (when the records are first created, they have a fake, long-past date as last scan date, such as 2000-01-01 to ensure that the bundle scanner can pick the up immediately). As it processes those records, it downloads the HTML content for them and extracts some bits of information, such as title, banner image, etc. Bundles can be temporary, as Steam may offer a certain bundle for a limited time only. When it encounters such a bundle, it also marks it as inactive, but these will be re-scanned in subsequent runs in case they are re-enabled.

To ensure that multiple instances of the bundle scanner can run concurrently without duplicated work, they insert reservations into Redis as they get the to-be-scanned items from the database. These reservations are just keys without any useful value, but with TTL set on them. The presence or absence of such keys specify whether the bundle with a given Id is scanned by another instance. When the scanning is complete, the DB record is updated, marking is as "scanned today", so other instances won't consider it for processing. If the process stops before fully processing a record though, then the corresponding Redis key's TTL will eventually expire. This means that if the DB record is not yet updated, then any subsequent queries will return that item and the expiration of the Redis reservation also enables reprocessing.

## SteamScrapper.SubScanner
