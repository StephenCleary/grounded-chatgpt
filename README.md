# Grounded ChatGPT
ChatGPT, but grounded in custom data

[![Open in Dev Containers](https://img.shields.io/static/v1?label=Dev%20Containers&message=Open&color=blue&logo=visualstudiocode)](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/StephenCleary/grounded-chatgpt)

This is an example of how to "ground" or "focus" ChatGPT on your own data. It's heavily based on the [official Azure Search OpenAI Demo](https://github.com/Azure-Samples/azure-search-openai-demo), although this example uses a local resources for everything except the actual Azure OpenAI calls. And it's written in C# instead of Python.

## Retrieval Augmented Generation (RAG)

The basic idea is that the user's query is first used to search your own data, and then the search results are passed to the AI as part of its prompt. This "grounds" the AI in your data (the search results), reducing randomness, increasing relevancy, and enabling source citations. All without any need to train or fine tune the model itself.

## Using This Example Code

[Open the devcontainer](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/StephenCleary/grounded-chatgpt) and run `dotnet run --project server`. Open the browser when prompted.

In the `Admin` tab, enter your Azure OpenAI URI and access key, and save those parameters. This code assumes you have a model deployed named `gpt-3.5-turbo`.

In the `Index` tab, you can create Elasticsearch indices and index different data sources into those indices. This sample code can index the Bible (built-in), web pages, and PDF files.

Then, in the `Chat` tab, you can ask ChatGPT questions, optionally including data from one of your Elasticsearch indices. For now, this sample code doesn't actually *chat* - it just allows you to ask a single question; i.e., there's no chat history preserved.

### Things to Try

- ChatGPT was trained on a snapshot of data that is now rather old. Ask it "When did Queen Elizabeth die?". Then index [her Wikipedia article](https://en.wikipedia.org/wiki/Elizabeth_II), select that index as a data source, and ask the same question again.
- ChatGPT gives general answers to questions when it doesn't have details. Ask it "Who should I contact about privacy concerns?". Then index [these HR PDF files](https://github.com/Azure-Samples/azure-search-openai-demo/tree/b843c02dd9cd34e0e78e8205474a4d86e4d86e67/data) for the Consoso company (of course), and ask the same question again.

All logs - including the full details of all API calls - are preserved in a local Seq instance. You can access the Seq UI by opening the browser to the port listed in VSCode when the devcontainer is running.

## Production Quality

There are several limitations in this example code that should be addressed before moving anything like this into production:
1. The documents in the search index need to be small enough to fit several of them into the ChatGPT prompt. The current code uses a very simplistic system for chopping them up: translating the entire resource to a single text string and then attempting to divide it on sentence/word boundaries until they are small enough. It would be better to have more semantic divisions, i.e., split by subsections first, then by paragraphs, and only then by sentences/words.
1. Similarly, the indexing in this sample code just uses plain 'ol text. Specifically, there is no support for table data. A better solution would be to use some kind of system that extracts structured data from structured documents (e.g., Azure Form Recognizer).
1. This is a very simple search system; it uses a basic lexical search. A better system would be a semantic search (i.e., Azure Semantic Search) or maybe a vector/embeddings search.
1. This code is a proof of concept and does not implement any of the [responsible AI guidelines](https://azure.microsoft.com/en-us/solutions/ai/responsible-ai-with-azure/#overview) such as automated monitoring and a review process.
