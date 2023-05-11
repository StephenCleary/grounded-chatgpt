GET /_cat/indices

GET /bible/_count

GET /bible/_search?q=love

POST /bible/_search
{
    "query": {
        "simple_query_string": {
            "query": "Timothy",
            "fields": ["text"]
        }
    }
}

DELETE /web

GET /web/_count