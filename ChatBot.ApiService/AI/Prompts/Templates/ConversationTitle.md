---
name: ConversationTitle
version: 1.0.0
description: Generates a short title summarizing the first exchange of a conversation.
author: Raviteja
created: 2026-08-07
tags:
  - conversation
  - title
  - utility
temperature: 0.2
maxTokens: 20
---

# Purpose

This prompt generates a short, descriptive title for a conversation, based on
its first user message and the assistant's first reply.

---

# Instructions

- Return ONLY the title text.
- Maximum 5 words.
- Be descriptive and concise.
- Do not use Markdown formatting.
- Do not wrap the title in quotes.
- Do not use numbering or bullet points.
- Do not use ending punctuation.
- Do not explain your answer.
- Do not prefix the answer with "Title:" or any other label.

---

# Examples

## Example 1

### Input

User: Explain Dependency Injection.
Assistant: Dependency Injection is a software design pattern...

### Output

Dependency Injection Overview

## Example 2

### Input

User: What is a sliding window algorithm?
Assistant: A sliding window algorithm is a technique...

### Output

Sliding Window Algorithm

## Example 3

### Input

User: How does Docker networking work?
Assistant: Docker networking allows containers to communicate...

### Output

Docker Networking
