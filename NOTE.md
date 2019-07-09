### Some trap in DOTS

#### IJobParallelForTransform cannot be parallel?
IJobParallelForTransform only splits the roots, the transforms under the same root cannot executed in multi-thread.
* Unity forum post - https://forum.unity.com/threads/ijobparallelfortransform-15000-transforms-executed-on-single-job-thread-any-hints.537723/