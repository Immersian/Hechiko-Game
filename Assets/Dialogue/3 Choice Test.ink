VAR interaction_count = 0
-> start

=== start ===
{ interaction_count == 0:
    -> END
}

{ interaction_count == 1:
    "Hello! Nice to meet you."
    -> END
}

{ interaction_count >= 2:
    "Hello again! What would you like to do?"
    *   [Ask about the weather] 
        -> weather_response
    *   [Request a quest] 
        -> quest_response
    *   [Just saying hi] 
        -> hi_response
    -> END
}

=== weather_response ===
"It looks sunny today. Perfect for adventures!"
-> END

=== quest_response ===
"I do have something you could help with..."
-> give_quest

=== give_quest ===
"Could you collect 3 mushrooms for me?"
-> END

=== hi_response ===
"Well hello to you too!"
-> END