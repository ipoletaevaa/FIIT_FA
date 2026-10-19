# Простые итерации (метод Якоби) и метод Зейделя.
# Начальное приближение — нулевой вектор.
# Остановка: бесконечная норма разности соседних приближений меньше eps.
# Бесконечная норма — это максимум модулей координат.

from lu import solve_system


def infinity_norm(delta):
    value = 0.0
    for item in delta:
        if abs(item) > value:
            value = abs(item)
    return value


def simple_iterations(a, b, eps, max_iter=100000):
    n = len(a)
    x = [0.0] * n

    for iteration in range(1, max_iter + 1):
        new_x = [0.0] * n
        for i in range(n):
            # В простой итерации вся сумма берётся из предыдущего приближения.
            total = b[i]
            for j in range(n):
                if j != i:
                    total -= a[i][j] * x[j]
            new_x[i] = total / a[i][i]

        diff = []
        for i in range(n):
            diff.append(new_x[i] - x[i])
        x = new_x
        if infinity_norm(diff) < eps:
            return x, iteration

    raise RuntimeError("простые итерации не сошлись за отведённое число шагов")


def seidel(a, b, eps, max_iter=100000):
    n = len(a)
    x = [0.0] * n

    for iteration in range(1, max_iter + 1):
        old_x = x[:]
        for i in range(n):
            # В методе Зейделя уже посчитанные новые x[0]..x[i-1] сразу идут в сумму.
            total = b[i]
            for j in range(n):
                if j != i:
                    total -= a[i][j] * x[j]
            x[i] = total / a[i][i]

        diff = []
        for i in range(n):
            diff.append(x[i] - old_x[i])
        if infinity_norm(diff) < eps:
            return x, iteration

    raise RuntimeError("метод Зейделя не сошёлся за отведённое число шагов")


def iterative_methods(a, b, eps):
    simple_x, simple_iters = simple_iterations(a, b, eps)
    seidel_x, seidel_iters = seidel(a, b, eps)
    reference = solve_system(a, b)["x"]

    if seidel_iters < simple_iters:
        faster = "Зейдель"
    elif simple_iters < seidel_iters:
        faster = "простые итерации"
    else:
        faster = "одинаково"

    return {
        "eps": eps,
        "simple_x": simple_x,
        "simple_iters": simple_iters,
        "seidel_x": seidel_x,
        "seidel_iters": seidel_iters,
        "reference": reference,
        "faster": faster,
    }
