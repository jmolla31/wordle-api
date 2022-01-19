# wordle-api

Api project for wordle clones.

This repository tries to provide a very simplistic api that any wordle-like app can use as a data source, or as a base for rolling out a custom backend.

A live deployment of this api can be found at: http://wordleapi.azurewebsites.net/api/{endpoints}

## Data source

Current data source contains english words with 5, 6 and 7 characters. Support for multiple languages is WIP.

## Responses

To maintain simplicty, all responses are represented as a combination of an HTTP status code (200 or 400) + a string value (word or an result string code).


## Endpoints

### /random?size=6

Get a random word from the source dictionary. Desired word size (lenght) can be specified using the 'size' query paremeter (current accepted values are 5, 6 and 7) 

Possible responses are:

- **HTTP 200 OK**: All good, response body will contain the retrieved word. 
- **HTTP 400 BadRequest**: Size parameter validation failed, response body will contain the error code.
- **HTTP 500 Internal Server Error**: Yeah, something went kachunk.

### /daily?size=6

Get today's daily word from the dictionary. This endpoint will return the same value for all calls made in a given day (resets at 00:00 UTC).
Desired word size (lenght) can be specified using the 'size' query paremeter (current accepted values are 5, 6 and 7) 

Possible responses are:

- **HTTP 200 OK**: All good, response body will contain the retrieved word. 
- **HTTP 400 BadRequest**: Size parameter validation failed, response body will contain the error code.
- **HTTP 500 Internal Server Error**: Yeah, something went kachunk.


### /check?input=earth

Check that the input word is valid. Input values are trimmed and normalized to lowercase before checking.

Possible responses are:

- **HTTP 200 OK**: Depending on wether the input word is in the dictionary, the response body will contain either the 'OK' or 'not_found' string codes.
- **HTTP 400 BadRequest**: Input word validation error (null or invalid lenght), response body will contain the error code.
- **HTTP 500 Internal Server Error**: Yeah, something went kachunk.
