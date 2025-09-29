VAR interaction_count = 0 
-> start
=== start ===
{ interaction_count == 0:
-> END
}
{ interaction_count == 1:
Paw-some to meet you Hachiko! I'm Tanuki >:)
    *   [Hello Tanuki] 
        -> Basic
    -> END
}
{ interaction_count == 2:
Can't get enough of my puns?
    -> DONE
}
{ interaction_count == 3:
Go purr-sue your owner, bruh
    -> DONE
}
{ interaction_count >= 4:
Uh... Go get your hooman Hachiko 
    -> DONE
}
=== Basic ===
I'm sure you are eager to find your owner.

Just head right — but the cats will purr-sist on holding you back.
       *[They won't] 
        -> Finish
       *[Great...]
        -> Finish
-> END

=== Finish ===
I woof you all the best ;)
       *[This guy pmo] 
        -> DONE
       *[I like this little guy]
        -> DONE
->DONE


