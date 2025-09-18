# Prompt Engineering Package

Questo package ha l’obiettivo di permettere al cliente di personalizzare il comportamento di un modello AI tramite la definizione di prompt strutturati.  
Il cliente potrà inserire direttamente i testi e le viste da utilizzare, senza modificare il codice.

---

## Struttura del Prompt

Il package supporta i seguenti elementi:

1. **System Prompt**  
   - Definisce il comportamento generale dell’AI.  
   - Esempio: *"Sei un assistente che risponde in maniera tecnica e concisa."*  
   - Campo da compilare: `system_prompt`.

2. **User Prompt**  
   - Rappresenta la richiesta diretta dell’utente.  
   - È il contenuto dinamico che arriva durante ogni chiamata al modello.  
   - Campo gestito: `user_prompt`.

3. **Context / Viste**  
   - Blocchi di informazione aggiuntiva a cui l’AI deve fare riferimento.  
   - Possono essere documenti, dati di business, FAQ.  
   - Campo da compilare: `context_views`.

4. **Few-Shot Examples**  
   - Esempi di input e output per guidare stile e comportamento.  
   - Campo opzionale: `few_shot_examples`.

5. **RAG (Retrieval Augmented Generation)** *(opzionale)*  
   - Possibilità di collegare fonti esterne (database, API, knowledge base).  
   - Campo: `retrieval_sources`.

---

## Configurazione

Il cliente compila un file di configurazione (es. `config.yaml`) con i propri testi:

```yaml
system_prompt: >
  Sei un assistente specializzato in [dominio].
  
context_views:
  - "Vista 1: Documento tecnico"
  - "Vista 2: Dati prodotto"
  
few_shot_examples:
  - input: "Domanda esempio"
    output: "Risposta esempio"

---

## Flusso di utilizzo

1. Il package carica il `system_prompt` e le `context_views`.
2. Integra gli `user_prompt` dinamici durante la sessione.
3. Se presenti, aggiunge `few_shot_examples`.
4. (Opzionale) Recupera dati da `retrieval_sources`.
5. Costruisce il prompt finale da passare al modello AI.
6. (Opzionale) Le risposte del modello possono essere inviate via email tramite `LoggerHelper.Sink.Email`.
