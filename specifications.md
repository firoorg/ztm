# ZTM Specifications

## Design

- Addresses managed by system:
  - Issuer.
  - Distributor.
  - Receive addresses.
- Receive addresses will be in the pool for reuse.
- The issuer will only create/destroy tokens. When token created it will transfer all created tokens to distributor.
- The balance will be the smallest unit which is 8 precision (1 mean 0.00000001).
- Each token transaction will need to pay fee.
- All fee will be pay in Zcoin balance (XZC). The unit will always in satoshi.
- All addresses managed by system must have enough Zcoin balance in order to pay fee for token transaction.

## REST APIs

- Some request can specify callback URL with `X-Callback-URL` header to let the system do a request to that URL later. If not specified, no callback will be made.
- In the response of the request that specified callback URL (and it supported) there will be `X-Callback-ID` header to indicated the identifier of the callback that will be made in the future. Also, HTTP status code will be `202` instead of `200` regardless the present of `X-Callback-ID`.
- Callback will be done with `POST` method. Each callback will have `X-Callback-ID` header to indicated what callback is being made and `X-Callback-Status` header to indicated data in the request body.
- For a success callback the status will be `success` and error callback will be `error`. Some callback may also have additional status specific to it.
- There is one special callback `update` to indicated the progress of operation. This callback may do multiple times which is different from other callback that will be the last callback.
- All error callback will have `message` field for the error reason in English. All remaining field is operation specific.

### Issue Tokens

```
POST /issue-tokens
```

Sample request to issue 1,000,000 tokens:

```json
{
  "amount": 100000000000000,
  "note": "Initial tokens."
}
```

`success` callback:

```json
{
  "issuing_tx": "468b649441ca7fca165aed72bc4b69e1546dc0e1670d15137c684d49534e1b2c",
  "transfer_tx": "7f32a54475a5da05a70fea560275b644be15fa84cdaa2a5cec70c56d20b0fad3"
}
```

`tokens-issuing-timeout` callback:

```json
{
  "tx": "468b649441ca7fca165aed72bc4b69e1546dc0e1670d15137c684d49534e1b2c"
}
```

`tokens-transfer-timeout` callback:

```json
{
  "tx": "468b649441ca7fca165aed72bc4b69e1546dc0e1670d15137c684d49534e1b2c"
}
```

### Transfer Tokens

```
POST /transfers
```

Sample request to transfer 1,000 tokens from distributor to address `aBydwLXzmGc7j4mr4CVf461NvBjBFk71U1`:

```json
{
  "amount": 100000000000,
  "destination": "aBydwLXzmGc7j4mr4CVf461NvBjBFk71U1"
}
```

Response:

```json
{
  "tx": "7f32a54475a5da05a70fea560275b644be15fa84cdaa2a5cec70c56d20b0fad3",
  "fee": 1000
}
```

`success` callback:

- No callback-specific data.

`tokens-transfer-timeout` callback:

- No callback-specific data.

### Receive Tokens

```
GET /receive-address
```

Sample request to get an address to send 1,000 tokens to the system:

```
GET /receive-address?target_amount=100000000000
```

Response:

```json
{
  "address": "aENPzvNpNHENttzF7yZBmz5d2nAxBVRzXE"
}
```

`success` callback:

```json
{
  "received": 102000000000
}
```

`tokens-receive-timeout` callback:

```json
{
  "received": 50000000000
}
```

### Get Issuer Information

```
GET /issuer
```

Response:

```json
{
  "address": "aENPzvNpNHENttzF7yZBmz5d2nAxBVRzXE",
  "balance": 5000000000
}
```

### Get Distributor Information

```
GET /distributor
```

Response:

```json
{
  "address": "aENPzvNpNHENttzF7yZBmz5d2nAxBVRzXE",
  "balance": 5000000000,
  "tokens": 1000000000000000
}
```

### Get Receive Address Information

```
GET /receive-addresses
```

Response:

```json
[
  {
    "address": "aENPzvNpNHENttzF7yZBmz5d2nAxBVRzXE",
    "balance": 1000000000,
    "tokens": 500000000
  }
]
```
