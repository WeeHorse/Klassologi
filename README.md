# Klassologi

En social applikation för skolklasser. Krav, beslut och öppna frågor finns i [Specifications](Specifications/).

## Lokal utveckling

### Databas (MySQL i Docker)

Kräver Docker med Compose.

1. Skapa `.env` från mallen och byt ut lösenorden:

   ```sh
   cp .env.example .env
   ```

   `.env` ignoreras av git. Lägg aldrig riktiga lösenord i `.env.example`.

2. Starta databasen:

   ```sh
   docker compose up -d
   ```

   Databasen är redo när `docker compose ps` visar `healthy`. Den nås bara från den egna datorn, på `127.0.0.1:3306` (porten kan ändras med `MYSQL_PORT` i `.env`).

3. Anslut med en klient:

   ```sh
   docker compose exec mysql sh -c 'mysql -u "$MYSQL_USER" -p"$MYSQL_PASSWORD" "$MYSQL_DATABASE"'
   ```

Stoppa med `docker compose down`. Datat ligger kvar i en Docker-volym. `docker compose down -v` raderar datat och ger en tom databas vid nästa start.

Anslutningssträng för .NET (MySqlConnector):

```
Server=127.0.0.1;Port=3306;Database=klassologi;User ID=klassologi;Password=<MYSQL_PASSWORD>;SslMode=Required
```

Schema och migreringar finns ännu inte.
