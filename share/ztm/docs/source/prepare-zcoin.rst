Prepare Zcoin
=============

Before |project_name| can be used on Zcoin we need to do some pre-configurations for Zcoin.

Create Token
------------

If you don't have a token on Exodus yet. You need to create it first. Refer to the documentation of Zcoin for the
instructions. The token **MUST** be a managed token. |project_name| will not work with a fixed token. You will get the
address that own the token from this step, note it somewhere due to we will need it later. From now on we will call this
address as *Issuer*.

Create Distributor Address
--------------------------

|project_name| needs a new additional address in order to operate. Whenever |project_name| issue a new token it will
issue to this address instead of the token owner. We will name this adddress as *Distributor*. You can create a new
address with ``getnewaddress`` command. We will need this address later so note it somewhere.

Configure |project_name|
------------------------

We need to update |project_name|'s configuration so it know how to interact with Zcoin. The following is an example:

.. code-block:: json

  {
    "Zcoin": {
      "Network": {
        "Type": "Mainnet"
      },
      "Rpc": {
        "Address": "http://127.0.0.1:28888",
        "UserName": "zcoin",
        "Password": "zcoin"
      },
      "Property": {
        "Id": 3,
        "Type": "Divisible",
        "Issuer": "Mainnet:aHEog3QYDGa8wH4Go9igKLDFkpaMsi3btq",
        "Distributor": "Mainnet:aLTSv7QbTZbkgorYEhbNx2gH4hGYNLsoGv"
      },
      "ZeroMq": {
        "Address": "tcp://127.0.0.1:28332"
      }
    }
  }

The available values for Network Type are:

- Mainnet
- Testnet
- Regtest

The default port for RPC is difference on each network. Consult the documentation of Zcoin for the list of default RPC
port. Put the username and password you have configured from :doc:`/install-zcoin` into *UserName* and *Password*.

For *Property* block, you can find *Id* of your token with ``exodus_listproperties`` command. The available values
for *Type* are:

- Indivisible
- Divisible

Put the addresses you note from the above into *Issuer* and *Distributor*.

For *ZeroMq*, the values is depend on what you configured in :doc:`/install-zcoin`.
