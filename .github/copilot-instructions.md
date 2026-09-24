# Copilot Instructions

## Project Guidelines
- Use CompanionPromptBuilder as the centralized prompt builder for all prompt-related behavior, including activity-agent prompts; do not use ActivityPromptBuilder.

## Configuration Guidelines
- For mobile Google Cloud Text-to-Speech configuration, use a durable credential injection method instead of a fixed short-lived OAuth access token.