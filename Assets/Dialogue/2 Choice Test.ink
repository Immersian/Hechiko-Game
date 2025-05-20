VAR interaction_count = 0 
-> start
=== start ===
{ interaction_count == 0:
-> END
}
{ interaction_count == 1:
    "Hello! This is our first meeting."
    -> END
}

{ interaction_count == 2:
    "Nice to see you again!"
    *   [Friend?] 
        -> friends_path
    *   [Just passing by] 
        -> passing_by
}

{ interaction_count >= 3:
    "Oh, it's you again."
    "Goodluck on your quest!"
    -> END
}

=== friends_path ===
"Great! Let's be friends!"
-> END

=== passing_by ===
"Alright, see you around."
-> END