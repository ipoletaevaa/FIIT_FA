# Метод прогонки для трёхдиагональной системы.
# Полную матрицу не храним: только нижнюю диагональ, главную, верхнюю и правую часть.
#
# Уравнение номер i (нумерация с нуля):
#   lower[i] * x[i-1] + main[i] * x[i] + upper[i] * x[i+1] = rhs[i]
# Крайние отсутствующие коэффициенты равны нулю: lower[0] = 0, upper[n-1] = 0.


def thomas_method(lower, main, upper, rhs):
    n = len(main)
    # x[i] = alpha[i] * x[i+1] + beta[i]
    # Для последнего уравнения upper[n-1] = 0, поэтому alpha[n-1] = 0 и x[n-1] = beta[n-1].
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
