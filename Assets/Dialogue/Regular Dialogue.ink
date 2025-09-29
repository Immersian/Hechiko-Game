VAR interaction_count = 0 
-> start
=== start ===
{ interaction_count == 0:
-> END
}
{ interaction_count == 1:
The Big Bad BAKENEKO is up ahead
    *   [Who?] 
        -> Basic
    -> END
}
{ interaction_count >= 2:
Need to know it's weakspot?
    *   [Yes] 
        -> Weakspot
    *   [Nope]
        -> End3
}
=== Basic ===
A very fur-ious cat out to stop you
    *   [Know it's weakspot?] 
        -> Weakspot
    *   [I won't let him]
        -> End

=== Weakspot === 
Aim for that red third eye on her face

Or use your drum to stun her

    *   [Thank you Tanuki]
        ->End2

=== End ===
Bakeneko's a her but you got this >:)
->END

=== End2 ===
You got this >:)
->END

=== End3 ===
Bruh
->END
