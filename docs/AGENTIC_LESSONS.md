# Agentic AI Lessons & Principles

> [!NOTE]
> These principles were discovered and validated during agentic AI experiments on the Enlisted project (2024-2026). These architectural lessons remain the "Gold Standard" for building reliable agentic systems.

## 1. Contextual Retrieval (RAG)

The reliability of an agent is bounded by the quality of its context. Simple vector search defaults (k=4) are insufficient for complex coding tasks.

### The "Gold Standard" Pipeline

We achieved **+67% retrieval accuracy** using this specific pipeline:

1. **Semantic Chunking**: Split text at logical boundaries (classes/functions) rather than arbitrary token counts.
    * *Lesson*: Code needs to be chunked by AST (Abstract Syntax Tree), not whitespace.
2. **Contextualization**: Prepend every chunk with 2-3 sentences of AI-generated context *before* embedding.
    * *Why*: A chunk saying `return this._hero;` is meaningless on its own. Contextualized: *"This chunk acts as the getter for the Hero property in the EnlistmentManager class..."* makes it retrievable.
    * *Reference*: [Anthropic Contextual Retrieval](https://www.anthropic.com/news/contextual-retrieval)
3. **Hybrid Search (RRF)**: Combine **Vector Search** (semantic) + **BM25** (keyword) using Reciprocal Rank Fusion.
    * *Why*: Vector misses exact identifiers (error codes, specific variable names). BM25 misses concepts ("player progression"). Together they cover blind spots.
4. **Cohere Reranking**: Retrieve huge datasets (top-50) and use a cross-encoder (Cohere Rerank v3.5) to sort them by true relevance.
    * *Impact*: This single step provided the largest jump in quality (+18%).
5. **FILCO (Filter Context)**: Post-retrieval filtering to remove "noise" (empty spans, logs, generic boilerplate) before feeding to the LLM.

## 2. Agent Reliability Patterns

### Single-Agent vs. Multi-Agent Chat

* **Discovery**: "Chatting" agents (A talks to B) is fun but fragile. It incurs massive token costs and often results in loop-de-loops.
* **Solution**: **Single-Agent Flows**. Break a complex task into discrete steps, where each step is executed by a *single* agent with a specific toolset, coordinated by deterministic code.
  * *Benefit*: Deterministic control flow, easy debugging, 80% cost reduction.

### Pydantic Output Fixers

* **Problem**: LLMs often fail to output strict JSON, especially when "distracted" by tool usage.
* **Solution**: Do not rely on the LLM to "get it right". Wrap the output parsing in a **Retry Loop** that:
    1. Detects schema validation failure.
    2. Feeds the error *plus* a concrete JSON example of the schema back to the agent.
    3. Retries (up to 3x).
  * *Result*: Went from ~70% success to near 100% reliability for structured data tasks.

### Generator-Critic Pattern

* **Pattern**: Never let an agent mark its own homework.
* **Implementation**:
    1. **Agent A (Generator)**: "Here is the plan to fix the bug."
    2. **Agent B (Critic)**: "I am a QA engineer. Find flaws in this plan." (Given specific rubrics).
  * *Key*: The Critic must have *different tools* or *different instructions* (e.g., "Assume the database is stale").

## 3. Database-Driven Discovery

* **Problem**: Agents hallucinate file paths or system names.
* **Solution**: **Lazy-Sync SQLite**.
  * Create a local SQLite database that mirrors the codebase structure (System names, File paths, dependency graphs).
  * Update it automatically when the agent runs tools.
  * Agents query *SQL* (fast, exact) to find "Where is the Reputation System?" instead of guessing file paths or grepping.

## 4. Tool Budgeting

* **Principle**: Constrain agents to prevent "doom-scrolling" the codebase.
* **Implementation**: Assign a strict "Tool Budget" (e.g., max 10 searches) per step.
  * *Effect*: Forces the agent to think strategically ("I have 3 searches left, I better check the index first") rather than flailing.
