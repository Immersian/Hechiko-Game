VAR interaction_count = 0 
-> start
=== start ===
{ interaction_count == 0:
-> END
}
{ interaction_count == 1:
    Hi asshole :)
    Need some help for figuring bros clunky game out?
    *   [Nah go away] 
        -> End
    *   [Yes pls] 
        -> Basic
    -> END
}

{ interaction_count == 2:
    Hello again asshole, need the tutorial again?
    *   [Nah go away] 
        -> End
    *   [Yes pls] 
        -> Basic
}
{ interaction_count == 3:
    Im shreknant
    *   [uh bye] 
        -> End
    *   [Shut up bruh, tutorial pls] 
        -> Basic
}
{ interaction_count > 3:
    Alright, this dialogues gonna loop now :(
    *   [K go away] 
        -> End
    *   [Tutorial pls] 
        -> Basic
}
=== Basic ===
Ight, what would u like to learn?
       *[Nvm dont want] 
        -> End
       *[Parrying]
        -> Parrying
       *[Attacking]
        -> Attacking
       *[Movement] 
        -> Movement
-> END

=== End ===
"Alright, see you around."
-> END

=== Movement ===
Left Joystick for Movement, R1 to Dash and X is Jumping
Yea I think I got everything
I think...
-> Basic

=== Attacking ===
Square for a Normal Attack and Triangle for an Upward Attack.
Dashing (R1) is also an attack
Feel free to test it out on the dummy
-> Basic

=== Parrying ===
Press L1 to Parry, you will have to time it right before an attack.
or u can just spam L1 because the Kurtis didnt do shit to fix this yet.
You take no damage and build up your Bark Fury (Special attack).
-> Basic



