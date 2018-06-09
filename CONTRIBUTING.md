### Issues

Both ideas and bugs are welcome.

If you think it's worth submitting — it's worth submitting. Just check it's not already submitted.
If you think it's not worth submitting, but it's relevant — submit anyway.
Cosmetic bugs are still bugs, impossible ideas might be possible.

### Code

Code contributions are very welcome — but please read these notes:

1. It's always recommended to find or submit an issue first, and comment that you want to do it.
This is especially important if you want to implement new feature.
Your effort might be lost if it's already in progress, or if there is a disagreement about approach, etc.

2. I have very specific opinions about UI design, code structure, patterns, features, etc.
I might adjust or rewrite the code your submitted to match these ideas.
This doesn't mean your code was wrong — my changes are often very subjective.

3. At the moment, there is no CLA (Contributor License Agreement), however I do intend to have one.
The idea is to keep a full ownership over SharpLab and be able to change license in the future.
Please keep this in mind if you dislike a possibility of a license change or, for example, paid features.

#### Getting started

1. After you cloned the repo, run `sl setup` to do initial project setup.
2. If tests fail on `NullReferenceException` in `OptionsService`, just rerun them (Roslyn issue, looking into it).
3. Code style is in `.editorconfig` — you should get 4-space indents and K&R braces automatically.
4. If you run into an issue with build/tests/etc -- please report it on GitHub.

#### Pull requests

Please raise all pull requests against `edge` instead of `master`.  
Before change is merged to master, it is tested on the test site (deployed from `edge`).