VAR interaction_count = 0 
-> start
=== start ===
{ interaction_count == 0:
-> END
}
{ interaction_count == 1:
    Need some help for figuring the clunky game out?
    *   [Nah go away] 
        -> End
    *   [Yes pls] 
        -> Basic
    -> END
}

{ interaction_count == 2:
    Hello, need the tutorial again?
    *   [Nah go away] 
        -> End
    *   [Yes pls] 
        -> Basic
}
{ interaction_count == 3:
    Tutorial?
    *   [uh bye] 
        -> End
    *   [Yep] 
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
       *[Special Attack] 
        -> SpecialAttack
-> END

=== End ===
"Ok vro"
-> END

=== SpecialAttack ===
Your special attack is built by parries. Parry 5 times and press LT.
This will stun all the enemies in the area. 
For this user test it only works for the grounded enemy.
-> Basic

=== Attacking ===
Square for a Normal X and Y for an Upward Attack.
Dashing (RB) is also an attack
Feel free to test it out on the dummy
-> Basic

=== Parrying ===
Press LB to Parry, you will have to time it right before an attack.
or u can just spam LB because the game designer didnt fix this yet.
You take no damage and build up your Special attack.
-> Basic



