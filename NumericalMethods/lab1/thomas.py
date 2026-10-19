def thomas_method(lower, main, upper, rhs):
    n = len(main)
    alpha = [0.0] * n
    beta = [0.0] * n

    alpha[0] = -upper[0] / main[0]
    beta[0] = rhs[0] / main[0]

    for i in range(1, n):
        denom = main[i] + lower[i] * alpha[i - 1]
        alpha[i] = -upper[i] / denom
        beta[i] = (rhs[i] - lower[i] * beta[i - 1]) / denom

    x = [0.0] * n
    x[n - 1] = beta[n - 1]
    for i in range(n - 2, -1, -1):
        x[i] = alpha[i] * x[i + 1] + beta[i]

    return alpha, beta, x
