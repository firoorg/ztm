# ZTM Specifications

## Design

- Addresses managed by system:
  - Issuer.
  - Distributor.
  - Receiving addresses.
- Receiving addresses will be in the pool for reuse.
- The issuer will only create/destroy tokens. When tokens are created it will transfer to the distributor.
- There are 3 types of balance in the system.
- The balance for a divisible property can have maximum 8 precision (e.g. `0.00000001`).
- The balance for an indivisible propery will be always integer (e.g. `500`).
- The balance for Zcoin (XZC) will always in a coin unit (e.g. `1` mean 1 XZC) which will can have maximum of 8 precision (e.g. `0.00000001` mean 1 Satoshi).
- Each token's operation need to pay fee.
- All fee will be pay in XZC.
- All addresses that managed by the system must have enough XZC in order to pay fee.

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

Sample request to issue 1,000,000 tokens

Indivisible property:

```json
{
  "amount": "1000000",
  "note": "Initial tokens."
}
```

Divisible property:

```json
{
  "amount": "1000000.00000000",
  "note": "Initial tokens."
}
```

Response:

```json
{
  "tx": "7f32a54475a5da05a70fea560275b644be15fa84cdaa2a5cec70c56d20b0fad3"
}
```

`success` callback:

```json
{
  "tx": "7f32a54475a5da05a70fea560275b644be15fa84cdaa2a5cec70c56d20b0fad3"
}
```

`tokens-issuing-timeout` callback:

```json
{
  "tx": "7f32a54475a5da05a70fea560275b644be15fa84cdaa2a5cec70c56d20b0fad3"
}
```

### Transfer Tokens

```
POST /transfers
```

Sample request to transfer 1,000 tokens from distributor to address `aBydwLXzmGc7j4mr4CVf461NvBjBFk71U1`

Indivisible property:

```json
{
  "amount": "1000",
  "destination": "aBydwLXzmGc7j4mr4CVf461NvBjBFk71U1"
}
```

Divisible property:

```json
{
  "amount": "1000.00000000",
  "destination": "aBydwLXzmGc7j4mr4CVf461NvBjBFk71U1"
}
```

With reference amount:

```json
{
  "amount": "1000",
  "destination": "aBydwLXzmGc7j4mr4CVf461NvBjBFk71U1",
  "reference_amount": "0.00000100"
}
```

Response:

```json
{
  "tx": "7f32a54475a5da05a70fea560275b644be15fa84cdaa2a5cec70c56d20b0fad3"
}
```

`success` callback:

```json
{
  "tx": "7f32a54475a5da05a70fea560275b644be15fa84cdaa2a5cec70c56d20b0fad3"
}
```

`tokens-transfer-timeout` callback:

```json
{
  "tx": "7f32a54475a5da05a70fea560275b644be15fa84cdaa2a5cec70c56d20b0fad3"
}
```

### Receive Tokens

```
POST /receiving
```

Sample request to start receiving 4.9 divisible tokens to the system:

```json
{
  "target_amount": "4.9"
}
```

Response:

```json
{
  "address": "aENPzvNpNHENttzF7yZBmz5d2nAxBVRzXE",
  "deadline": "2020-01-31T18:25:43.511Z"
}
```

`success` callback:

```json
{
  "received": {
    "confirmed": "5.3000000",
    "pending": null
  }
}
```

`tokens-receive-timeout` callback:

```json
{
  "received": {
    "confirmed": "3.50000000",
    "pending": "1.00000000"
  }
}
```

### Get Issuer Information [NOT IMPLEMENTED]

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

### Get Distributor Information [NOT IMPLEMENTED]

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

### Create A Receiving Address

```
POST /receiving-addresses
```

Sample request to create a new receiving address:

```json
{
}
```

Response:

```json
{
  "address": "aENPzvNpNHENttzF7yZBmz5d2nAxBVRzXE"
}
```

### Get Receiving Addresses Information [NOT IMPLEMENTED]

```
GET /receiving-addresses
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
